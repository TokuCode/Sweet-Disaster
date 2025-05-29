using System;
using System.Collections.Generic;
using Code.Gameplay.Character.Command;
using Code.Gameplay.Character.Framework;
using Code.Helpers;
using Code.Helpers.Pipeline;
using Code.Networking.ClientPrediction;
using Code.Systems.Input;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Code.Gameplay.Character
{
    public class PlayerController : Controller
    {
        public static PlayerController Singleton;

        private IControl _input;
        public PlayerCommandInvoker Invoker { get; private set; }
        public Pipeline<InputPayload> InputPipeline { get; private set; } = new();
        
        public Rigidbody2D rigidbody { get; private set; }
        public CapsuleCollider2D collider { get; private set; }
        
        //Network general
        private NetworkTimer _networkTimer;
        private const float serverTickRate = 60f;
        private const int bufferSize = 1024;
        
        //Network client specific
        private CircularBuffer<StatePayload> _clientStateBuffer;
        private CircularBuffer<InputPayload> _clientInputBuffer;
        private StatePayload _lastServerState;
        private StatePayload _lastProcessedState;
        private ClientNetworkTransform _clientNetworkTransform;
        
        //Network server specific
        private CircularBuffer<StatePayload> _serverStateBuffer;
        private Queue<InputPayload> _serverInputQueue;

        [Header("Netcode")] 
        [SerializeField] private float _reconciliationCooldownTime = 1f;
        [SerializeField] private float _reconciliationThreshold = 2.5f;
        [SerializeField] private float _extrapolationLimit = .5f;
        [SerializeField] private float _extrapolationMultiplier = 1.2f;
        private CountdownTimer _reconciliationTimer;
        private CountdownTimer _extrapolationTimer;
        StatePayload _extrapolationState;
        
        private void Awake()
        {
            rigidbody = GetComponent<Rigidbody2D>();
            collider = GetComponent<CapsuleCollider2D>();
            
            _clientNetworkTransform = GetComponent<ClientNetworkTransform>();
            
            Invoker = new PlayerCommandInvoker(this);

            if (InputReader.Instance is IControl controls)
            {
                _input = controls;
            }
            
            _networkTimer = new NetworkTimer(serverTickRate);
            _clientStateBuffer = new CircularBuffer<StatePayload>(bufferSize);
            _clientInputBuffer = new CircularBuffer<InputPayload>(bufferSize);
            _serverStateBuffer = new CircularBuffer<StatePayload>(bufferSize);
            _serverInputQueue = new Queue<InputPayload>();

            _reconciliationTimer = new(_reconciliationCooldownTime);
            _extrapolationTimer = new(_extrapolationLimit);
            
            _reconciliationTimer.OnTimerStart += () => _extrapolationTimer.Stop();
            _extrapolationTimer.OnTimerStart += () =>
            {
                _reconciliationTimer.Stop();
                SwitchAutorityMode(AuthorityType.Server);
            };
            _extrapolationTimer.OnTimerStop += () =>
            {
                _extrapolationState = default;
                SwitchAutorityMode(AuthorityType.Client);
            };
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;

            if (Singleton != null)
            {
                Debug.LogError("Only one player can be spawned at a time");
                Destroy(gameObject);
                return;
            }
            
            Singleton = this;
            
            base.OnNetworkSpawn();
        }
        
        protected override void Update()
        {
            _networkTimer.Update(Time.deltaTime);
            _reconciliationTimer.Tick(Time.deltaTime);
            _extrapolationTimer.Tick(Time.deltaTime);
            Extrapolate();
            
            if (!IsOwner) return;
            
            if (Invoker.CenterPosition.Request(out Vector3 centerPosition).success)
            {
                InputReader.Instance.CachePlayerPosition(centerPosition);
            }
            
            base.Update();
        }

        protected override void FixedUpdate()
        {
            while (_networkTimer.ShouldTick())
            {
                HandleClientTick();
                HandleServerTick();
            }
            
            Extrapolate();
            
            base.FixedUpdate();
        }

        private void HandleServerTick()
        {
            if(!IsServer) return;
            
            var bufferIndex = -1;
            InputPayload inputPayload = default;
            while (_serverInputQueue.Count > 0)
            {
                inputPayload = _serverInputQueue.Dequeue();
                bufferIndex = inputPayload.tick % bufferSize;
                StatePayload statePayload = default;

                if (IsHost)
                {
                    statePayload = ProcessState(inputPayload);
                    _serverStateBuffer.Add(statePayload, bufferIndex);
                    SendStateToClientRpc(statePayload);
                    continue;
                }
                
                InputPipeline.Process(ref inputPayload);
                statePayload = ProcessState(inputPayload);
                _serverStateBuffer.Add(statePayload, bufferIndex);
            }

            if (bufferIndex == -1) return;
            SendStateToClientRpc(_serverStateBuffer.Get(bufferIndex));
            HandleExtrapolation(_serverStateBuffer.Get(bufferIndex), CalculateLatencyMiliseconds(inputPayload));
        }

        private void HandleClientTick()
        {
            if(!IsClient || !IsOwner) return;

            var currentTick = _networkTimer.CurrentTick;
            var bufferIndex = currentTick % bufferSize;

            InputPayload inputPayload = new()
            {
                tick = currentTick,
                timestamp = DateTime.Now,
                networkObjectId = NetworkObjectId,
                moveInput = _input.Move
            };
            
            _clientInputBuffer.Add(inputPayload, bufferIndex);
            SendInputToServerRpc(inputPayload);
            
            InputPipeline.Process(ref inputPayload);
            StatePayload statePayload = ProcessState(inputPayload);
            _clientStateBuffer.Add(statePayload, bufferIndex);
            HandleServerReconcilitation();
        }

        [ServerRpc]
        void SendInputToServerRpc(InputPayload input)
        {
            _serverInputQueue.Enqueue(input);
        }

        [ClientRpc]
        void SendStateToClientRpc(StatePayload statePayload)
        {
            if(!IsOwner) return;
            _lastServerState = statePayload;
        }

        StatePayload ProcessState(InputPayload input)
        {
            return new()
            {
                tick = input.tick,
                networkObjectId = NetworkObjectId,
                position = transform.position,
                velocity = rigidbody.linearVelocity
            };
        }

        void HandleServerReconcilitation()
        {
            if (!ShouldReconcile()) return;

            float positionError;
            int bufferIndex;
            
            bufferIndex = _lastServerState.tick % bufferSize;
            if (bufferIndex - 1 < 0) return;
            
            StatePayload rewindState = IsHost ? _serverStateBuffer.Get(bufferIndex - 1) : _lastServerState;
            StatePayload clientState = IsHost ? _clientStateBuffer.Get(bufferIndex - 1) : _clientStateBuffer.Get(bufferIndex);
            positionError = Vector3.Distance(rewindState.position, clientState.position);

            if (positionError > _reconciliationThreshold)
            {
                ReconcileState(rewindState);
                _reconciliationTimer.Start();
            }
            
            _lastProcessedState = rewindState;
        }

        bool ShouldReconcile()
        {
            bool isNewServerState = !_lastServerState.Equals(default);
            bool isLastStateUndefinedOrDifferent = _lastProcessedState.Equals(default) 
                                                   || !_lastProcessedState.Equals(_lastServerState);
            
            return isNewServerState && isLastStateUndefinedOrDifferent && !_reconciliationTimer.IsRunning && !_extrapolationTimer.IsRunning;
        }

        void ReconcileState(StatePayload rewindState)
        {
            transform.position = rewindState.position;
            rigidbody.linearVelocity = rewindState.velocity;

            if (rewindState.Equals(_lastServerState)) return;
            
            _clientStateBuffer.Add(rewindState, rewindState.tick);
            int tickToReplay = _lastServerState.tick;

            while (tickToReplay < _networkTimer.CurrentTick)
            {
                int bufferIndex = tickToReplay % bufferSize;
                InputPayload inputPayload = _clientInputBuffer.Get(bufferIndex);
                InputPipeline.Process(ref inputPayload);
                StatePayload statePayload = ProcessState(inputPayload);
                _clientStateBuffer.Add(statePayload, bufferIndex);
                tickToReplay++;
            }
        }

        void SwitchAutorityMode(AuthorityType mode)
        {
            _clientNetworkTransform.authority = mode;
            bool shouldSync = mode == AuthorityType.Client;
            _clientNetworkTransform.SyncPositionX = shouldSync;
            _clientNetworkTransform.SyncPositionY = shouldSync;
        }
        
        static float CalculateLatencyMiliseconds(InputPayload inputPayload) => (DateTime.Now - inputPayload.timestamp).Milliseconds / 1000f; 

        void Extrapolate()
        {
            if (IsServer && _extrapolationTimer.IsRunning)
            {
                transform.position += _extrapolationState.position.With(y: 0);
            }
        }

        void HandleExtrapolation(StatePayload latest, float latency)
        {
            if (ShouldExtrapolate(latency))
            {
                if (_extrapolationState.position != default)
                {
                    latest = _extrapolationState;
                }
                
                var posAdjustment = latest.velocity * (1 + latency * _extrapolationMultiplier);
                _extrapolationState.position = posAdjustment;
                _extrapolationState.velocity = latest.velocity;
                _extrapolationTimer.Start();
            }
            else
            {
                _extrapolationTimer.Stop();
            }
        }
        
        bool ShouldExtrapolate(float latency) => latency < _extrapolationLimit && latency > Time.fixedDeltaTime;
    }
}