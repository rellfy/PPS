using System;
using UnityEngine;

namespace PPS {

    [Serializable]
    public abstract class Profile {

        protected GameObject gameObject;

        public virtual Transform Transform => this.gameObject.transform;
        public virtual GameObject GameObject => this.gameObject;

        protected Profile() { }

        protected Profile(GameObject gameObject) {
            this.gameObject = gameObject;
        }
    }
}