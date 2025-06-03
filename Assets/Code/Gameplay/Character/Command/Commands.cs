using UnityEngine;

namespace Code.Gameplay.Character.Command
{
    public class RequestCenterPosition : IRequest<Vector3>
    {
        private Transform transform;
        private CapsuleCollider2D capsule;
        
        public RequestCenterPosition(Transform transform, CapsuleCollider2D capsule)
        {
            this.transform = transform;
            this.capsule = capsule;
        }

        public Result Request(out Vector3 output)
        {
            if (capsule == null)
            {
                output = Vector3.zero;
                return new Result { success = false };
            }
            
            output = transform.position + Vector3.up * capsule.size.y / 2;
            return new Result { success = true };
        }
    }

    public class LocalScaleHandler : IRequest<Vector3>, ICommand<Vector3>
    {
        private Transform transform;

        public LocalScaleHandler(Transform transform)
        {
            this.transform = transform;
        }

        public Result Request(out Vector3 data)
        {
            data = transform.localScale;
            return new Result { success = true };
        }

        public Result Perform(Vector3 input)
        {
            if (input == Vector3.zero)
                return new Result { success = false} ;
            
            transform.localScale = input;
            return new Result { success = true };
        }
    }

    public class SizeHanlder : IRequest<Vector2>, ICommand<Vector2>
    {
        private CapsuleCollider2D capsule;

        public SizeHanlder(CapsuleCollider2D capsule)
        {
            this.capsule = capsule;
        }

        public Result Perform(Vector2 input)
        {
            if(input == Vector2.zero)
                return new Result { success = false };
            
            capsule.size = input;
            return new Result { success = true };
        }
        
        public Result Request(out Vector2 output)
        {
            if (capsule == null)
            {
                output = Vector2.zero;
                return new Result { success = false };
            }
            
            output = capsule.size;
            return new Result { success = true };
        } 
    }

    public class VelocityHandlder : IRequest<Vector2>, ICommand<Vector2>
    {
        private Rigidbody2D rigidbody;

        public VelocityHandlder(Rigidbody2D rigidbody)
        {
            this.rigidbody = rigidbody;
        }

        public Result Perform(Vector2 input)
        {
            rigidbody.linearVelocity = input;
            return new Result { success = true };
        }

        public Result Request(out Vector2 output)
        {
            if (rigidbody == null)
            {
                output = Vector2.zero;
                return new Result { success = false };
            }
            
            output = rigidbody.linearVelocity;
            return new Result { success = true };
        }
    }
    
    public class GravityScaleHanlder : IRequest<float>, ICommand<float>
    {
        private Rigidbody2D rigidbody;

        public GravityScaleHanlder(Rigidbody2D rigidbody)
        {
            this.rigidbody = rigidbody;
        }

        public Result Perform(float input)
        {
            rigidbody.gravityScale = input;
            return new Result { success = true };
        }

        public Result Request(out float output)
        {
            if (rigidbody == null)
            {
                output = default;
                return new Result { success = false };
            }
            
            output = rigidbody.gravityScale;
            return new Result { success = true };
        }
    }

    public struct AddForceParams
    {
        public Vector2 force;
        public ForceMode2D forceMode;

        public AddForceParams(Vector2 force, ForceMode2D forceMode)
        {
            this.force = force;
            this.forceMode = forceMode;
        }

        public AddForceParams(Vector2 direction, float force, ForceMode2D forceMode)
        {
            this.force = direction.normalized * force;
            this.forceMode = forceMode;
        }

        public AddForceParams(float hor, float ver, ForceMode2D forceMode)
        {
            force = new Vector2(hor, ver);
            this.forceMode = forceMode;
        }
    }

    public class AddForceCommand : ICommand<AddForceParams>
    {
        private Rigidbody2D rigidbody;

        public AddForceCommand(Rigidbody2D rigidbody)
        {
            this.rigidbody = rigidbody;
        }

        public Result Perform(AddForceParams input)
        {
            rigidbody.AddForce(input.force, input.forceMode);
            return new Result { success = true };
        }
    }
}