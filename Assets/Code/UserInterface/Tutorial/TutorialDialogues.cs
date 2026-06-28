using System.Collections;
using Code.Gameplay.Character;
using UnityEngine;
using TMPro;
using Code.Helpers.Singleton;
using Code.Gameplay.Tutorial;
using Code.Gameplay.Character.Features;
using Code.Networking.Session;
using Code.Systems.Input;

namespace Code.UserInterface.Tutorial
{
    public class TutorialDialogues : Singleton<TutorialDialogues>
    {
        [SerializeField] private GameObject bindings;
        
        [SerializeField] private string[] dialogueTexts;
        [SerializeField] private TMP_Text textContainer;
        [SerializeField] private float textSpeed;
        [SerializeField] private float autoContinueDelay;
        
        private int currentIndex;
        private bool waitForTrigger;

        private GameObject player;
        
        protected override void Awake()
        {
            base.Awake();
            
            if (!SessionManager.Instance.IsPracticeMode)
            {
                Destroy(gameObject);
                return;
            }
            
            TutorialActions.Instance.OnPlayerJumpedOrCrouched += CheckMovement;
            TutorialActions.Instance.OnPlayerShotABot += CheckAction;
            TutorialActions.Instance.OnPlayerShotABomb += CheckAction;
            TutorialActions.Instance.OnPlayerBlockedAShot += CheckAction;
            TutorialActions.Instance.OnPlayerDidAShieldBash += CheckAction;
            TutorialActions.Instance.OnPlayerBrokenOutOfStun += CheckAction;
        }

        private void Update()
        {
            TutorialActions.Instance.currentIndex = currentIndex;
            TutorialActions.Instance.waitForTrigger = waitForTrigger;
        }

        private void CheckMovement()
        {
            if (!TutorialActions.Instance.PlayerHasJumped || !TutorialActions.Instance.PlayerHasCrouched) return;
            IncreaseIndex(1);
            TriggerContinue();
        }

        private void CheckAction(bool actionBool)
        {
            if (!actionBool) return;
            IncreaseIndex(1);
            TriggerContinue();
        }
        
        private void Start()
        {
            if (PlayerPrefs.GetInt("TutorialPlayed") == 0)
            {
                StartCoroutine(GetPlayer());
                StartCoroutine(ShowDialogue()); 
                //PlayerPrefs.SetInt("TutorialPlayed", 1);
            }
        }

        private IEnumerator GetPlayer()
        {
            player = GameObject.FindGameObjectWithTag("P1");
            yield return new WaitUntil(() => player == GameObject.FindGameObjectWithTag("P1"));
        }

        public void IncreaseIndex(int amount) => currentIndex += amount;
        public int GetIndex() => currentIndex;

        public void TriggerContinue()
        {
            waitForTrigger = false;
            StartCoroutine(ShowDialogue());
        }

        private IEnumerator ShowDialogue()
        {
            //player.GetComponent<Movement>().BlockMovement();
            InputReader.Instance.SetControl(false);
            
            textContainer.text = "";

            foreach (char c in dialogueTexts[currentIndex])
            {
                textContainer.text += c.ToString();
                yield return new WaitForSeconds(textSpeed);
            }
            
            if (ShouldWait(currentIndex))
            {
                waitForTrigger = true;
                //player.GetComponent<Movement>().UnblockMovement();
                InputReader.Instance.SetControl(true);
            }
            else
            {
                yield return new WaitForSeconds(autoContinueDelay);
                
                currentIndex++;
                
                if (currentIndex >= dialogueTexts.Length)
                {
                    //player.GetComponent<Movement>().UnblockMovement();
                    InputReader.Instance.SetControl(true);
                    gameObject.SetActive(false);
                    bindings.SetActive(true);
                }
                
                if (currentIndex < dialogueTexts.Length)
                    StartCoroutine(ShowDialogue());
            }
        }

        private bool ShouldWait(int index)
        {
            return index == 1 ||
                   index == 2 ||
                   index == 4 ||
                   index == 7 ||
                   index == 9 ||
                   index == 12 ||
                   index == 14 ||
                   index == 17;
        }
    }
}