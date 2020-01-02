using System;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

namespace PPS {

    public abstract class Processor {

        public abstract void Update();
        public abstract void FixedUpdate();
        public abstract void LateUpdate();
    }

    public abstract class Processor<TSystem, TProfile> : Processor, IDisposable
    where TSystem : ISystem
    where TProfile : Profile {

        private bool isDisposed;
        private bool isProcessing;
        protected TSystem system;
        protected TProfile profile;
        protected List<Processor> subProcessors = new List<Processor>();

        public event EventHandler OnProcessingStart;
        public event EventHandler OnProcessingEnd;

        public TProfile Profile => this.profile;
        public TSystem System => this.system;
        protected virtual bool ProcessOnFixedUpdate => false;
        protected virtual bool ShouldProcess => true;

        protected Processor(TSystem system,  TProfile profile) {
            this.system = system;
            this.profile = profile;
        }

        ~Processor() {
            Dispose(false);
        }

        protected virtual void Process() { }

        public override void Update() {
            foreach (Processor subProcessor in this.subProcessors) {
                subProcessor.Update();
            }

            if (!ProcessOnFixedUpdate)
                TryProcess();
        }

        public override void FixedUpdate() {
            foreach (Processor subProcessor in this.subProcessors) {
                subProcessor.FixedUpdate();
            }

            if (ProcessOnFixedUpdate)
                TryProcess();
        }

        public override void LateUpdate() {
            foreach (Processor subProcessor in this.subProcessors) {
                subProcessor.LateUpdate();
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

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing) {
            if (this.isDisposed || !isDisposing)
                return;

            this.system.RemoveInstance(this);
            UnityEngine.Object.DestroyImmediate(this.profile.GameObject);
            this.isDisposed = true;
        }
    }
}
