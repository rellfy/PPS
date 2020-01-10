using System;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

namespace PPS {

    // TODO: Add all MonoBehaviour messages: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public interface IProcessor {
        void Update();
        void FixedUpdate();
        void LateUpdate();
    }

    /// <summary>
    /// An independent Processor which does not handle any data owned by itself.
    /// </summary>
    public abstract class Processor : IProcessor {

        private bool isProcessing;
        private readonly List<Processor> subProcessors = new List<Processor>();

        public event EventHandler OnProcessingStart;
        public event EventHandler OnProcessingEnd;

        protected virtual bool ProcessOnFixedUpdate => false;
        protected virtual bool ShouldProcess => true;
        public List<Processor> SubProcessors => this.subProcessors;

        protected virtual void Process() { }

        /// <summary>
        /// Processor.Start is called when its system has been fully initialised
        /// in respect to subsystems and dependencies and is ready to operate.
        /// </summary>
        public virtual void Start() { }

        public virtual void Update() {
            // Reverse loop due to possible Processor disposal.
            for (int i = this.subProcessors.Count; i-- > 0;) {
                this.subProcessors[i].Update();
            }

            if (!ProcessOnFixedUpdate)
                TryProcess();
        }

        public virtual void FixedUpdate() {
            // Reverse loop due to possible Processor disposal.
            for (int i = this.subProcessors.Count; i-- > 0;) {
                this.subProcessors[i].FixedUpdate();
            }

            if (ProcessOnFixedUpdate)
                TryProcess();
        }

        public virtual void LateUpdate() {
            // Reverse loop due to possible Processor disposal.
            for (int i = this.subProcessors.Count; i-- > 0;) {
                this.subProcessors[i].LateUpdate();
            }
        }

        private void TryProcess() {
            if (!ShouldProcess) {
                if (!this.isProcessing)
                    return;

                this.isProcessing = false;
                OnProcessingEnd?.Invoke(this, null);
                return;
            }

            if (!this.isProcessing) {
                this.isProcessing = true;
                OnProcessingStart?.Invoke(this, null);
            }

            Process();
        }
    }

    /// <summary>
    /// A Processor which is linked to its Profile.
    /// </summary>
    public abstract class Processor<TProfile> : Processor
        where TProfile : Profile {

        private bool isDisposed;
        private readonly TProfile profile;

        public TProfile Profile => this.profile;

        protected Processor(TProfile profile) {
            this.profile = profile;
        }

        ~Processor() {
            Dispose(false);
        }

        public virtual void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing) {
            if (this.isDisposed || !isDisposing)
                return;

            if (this.profile.GameObject != null)
                UnityEngine.Object.DestroyImmediate(this.profile.GameObject);

            foreach (IDisposable subProcessor in SubProcessors)
                subProcessor.Dispose();

            this.isDisposed = true;
        }
    }

    /// <summary>
    /// A Processor which is linked to its Profile and instantiating System.
    /// </summary>
    public abstract class Processor<TSystem, TProfile> : Processor<TProfile>
    where TSystem : ISystem
    where TProfile : Profile {

        private readonly TSystem system;

        public TSystem System => this.system;

        protected Processor(TSystem system, TProfile profile) : base(profile) {
            this.system = system;
        }

        protected override void Dispose(bool isDisposing) {
            base.Dispose(isDisposing);

            if (isDisposing)
                this.system.RemoveInstance(this);
        }
    }
}
