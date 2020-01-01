using System;
using System.Collections.Generic;
using UnityEngine;

namespace PPS {

    public abstract class Processor {

        public abstract void Update();
        public abstract void FixedUpdate();
    }

    public abstract class Processor<TSystem, TProfile> : Processor
    where TSystem : ISystem
    where TProfile : Profile {

        private bool isProcessing;
        protected TSystem system;
        protected TProfile profile;
        protected List<Processor> subProcessors = new List<Processor>();

        public event EventHandler OnProcessingStart;
        public event EventHandler OnProcessingEnd;

        public TProfile Profile => this.profile;
        protected virtual bool ProcessOnFixedUpdate => false;
        protected virtual bool ShouldProcess => true;

        protected Processor(TSystem system,  TProfile profile) {
            this.system = system;
            this.profile = profile;
        }

        /// <summary>
        /// Method called once all subsystems have been initialised.
        /// </summary>
        protected virtual void Start() { }

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
}
