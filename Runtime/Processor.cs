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
        public virtual void SetReady() { }

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
    /// A Processor which is linked to its Profile and instantiating System.
    /// </summary>
    public abstract class Processor<TSystem> : Processor, IDisposable
    where TSystem : ISystem {

        private bool isDisposed;
        private readonly TSystem system;
        private readonly GameObject gameObject;

        public TSystem System => this.system;
        public GameObject GameObject => this.gameObject;
        public Transform Transform => this.gameObject.transform;

        protected Processor(TSystem system, GameObject instance) {
            this.gameObject = instance;
            this.system = system;
            system.AddInstance(this);
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

            if (this.gameObject != null)
                UnityEngine.Object.DestroyImmediate(this.gameObject);

            foreach (IDisposable subProcessor in SubProcessors)
                subProcessor.Dispose();

            this.system.RemoveInstance(this);
            this.isDisposed = true;
        }
    }
}
