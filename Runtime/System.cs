using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PPS {

    public interface ISystem : IProcessor {
        bool IsReady { get; }
        string NewInstanceName { get; }
        Transform Transform { get; }
        GameObject InstancePrefab { get; }
        Processor DeployInstance { get; }
        event EventHandler<Type> InstanceDeployed;
        event EventHandler<Type> InstanceRemoved;
        event EventHandler Ready;
        void RemoveInstance(Processor processor);
    }

    internal static class System {

        /// <summary>
        /// Deploys a system instance.
        /// </summary>
        public static Processor DeployInstance(Type processorType, Type profileType, Type systemType, ISystem system, GameObject prefab, Transform parent, string instanceName) {
            GameObject instance = prefab != null ? UnityEngine.Object.Instantiate(prefab, parent) : null;

            if (prefab != null) {
                instance.name = instanceName;
                instance.transform.parent = parent;
            }

            // Deploy profile with extra parameters.
            Type[] profileConstructorTypes = new Type[] { typeof(GameObject) };
            object[] profileConstructorParams = new object[] { instance };
            Profile profile = profileType.GetConstructor(profileConstructorTypes)?.Invoke(profileConstructorParams) as Profile;

            if (profile == null) {
                // Try again adding System as the first constructor parameter.
                profileConstructorTypes = new Type[] { systemType, typeof(GameObject) };
                profileConstructorParams = new object[] { system, instance };
                profile = profileType.GetConstructor(profileConstructorTypes)?.Invoke(profileConstructorParams) as Profile;
            }

            // Deploy processor.
            Type[] processorConstructorTypes = { systemType, profileType };
            object[] processorConstructorParams = { system, profile };
            Processor processor = processorType.GetConstructor(processorConstructorTypes)?.Invoke(processorConstructorParams) as Processor;

            if (processor == null || profile == null)
                throw new Exception("Could not instantiate the instance's Processor or Profile from type.");

            return processor;
        }

        /// <summary>
        /// DeployInstance implementation with generic types.
        /// </summary>
        public static TProcessor DeployInstance<TProcessor, TProfile>(Type systemType, ISystem system, GameObject prefab, Transform parent, string instanceName)
        where TProcessor : Processor
        where TProfile : Profile {
            return (TProcessor)DeployInstance(typeof(TProcessor), typeof(TProfile), systemType, system, prefab, parent, instanceName);
        }
    }

    [Serializable]
    public abstract class System<TProcessor, TProfile> : MonoBehaviour, ISystem
    where TProcessor : Processor
    where TProfile : Profile {

        [SerializeField]
        private bool deployOnStartup;
        [SerializeField, Tooltip("Whether to automatically mark this System as ready during MonoBehaviour.Awake.\n\n" +
                                 "If false it is required to manually set this System as ready, or its instances will " +
                                 "not have Processor.Start invoked.")]
        private bool readyOnStartup = true;
        private bool isReady;
        [SerializeField]
        private GameObject instancePrefab;
        [SerializeField]
        private ScriptableObject constants;
        protected List<TProcessor> instances = new List<TProcessor>();
        private List<ISystem> subsystems = new List<ISystem>();

        Processor ISystem.DeployInstance => DeployInstance();

        public bool IsReady => this.isReady;
        public string NewInstanceName => $"{GetType().Name} instance #{this.instances.Count + 1}";
        public Transform Transform => transform; // Thanks, Unity.
        public GameObject InstancePrefab => this.instancePrefab;
        public ScriptableObject Constants => this.constants;

        public event EventHandler<Type> InstanceDeployed;
        public event EventHandler<Type> InstanceRemoved;
        public event EventHandler Ready;

        public virtual void Awake() {
            InstanceDeployed += UpdateSerializableInstances;
            InstanceRemoved += UpdateSerializableInstances;

            DeploySubsystems();

            if (this.deployOnStartup && this.instances.Count == 0)
                DeployInstance();

            if (this.readyOnStartup)
                SetReady();
        }

        protected void SetReady() {
            this.isReady = true;
            Ready?.Invoke(this, null);

            foreach (TProcessor processor in this.instances) {
                processor.Start();
            }
        }

        /// <summary>
        /// Since unity 2019 does not serialise generics, it is needed to provide a wrapper type in the class that inherits from this one.
        /// </summary>
        protected abstract void UpdateSerializableInstances(object sender, Type instanceType);

        protected virtual void DeploySubsystems() { }

        public TProcessor DeployInstance() {
            TProcessor processor = System.DeployInstance<TProcessor, TProfile>(GetType(), this, this.instancePrefab, transform, NewInstanceName);
            this.instances.Add(processor);

            if (this.isReady)
                processor.Start();

            InstanceDeployed?.Invoke(processor, GetType());

            return processor;
        }

        public void RemoveInstance(Processor processor) {
            this.instances.Remove((TProcessor)processor);
            InstanceRemoved?.Invoke(processor, GetType());
        }

        protected void DeploySubsystem<TSubsystem, TSProcessor, TSProfile>(ref TSubsystem subsystem)
        where TSProcessor : Processor
        where TSProfile : Profile
        where TSubsystem : Subsystem<TSProcessor, TSProfile> {
            // Only construct subsystem if it has not been serialized.
            if (subsystem == null)
                subsystem = typeof(TSubsystem).GetConstructor(new Type[] { })?.Invoke(new object[] { }) as TSubsystem;

            if (subsystem == null)
                throw new Exception("Could not invoke new subsystem from types given");

            subsystem.Transform = new GameObject(subsystem.GetType().Name).transform;
            subsystem.Transform.parent = transform;
            this.subsystems.Add(subsystem);
            subsystem.Awake(subsystem.Transform, this);
        }

        public virtual void Update() {
            // Reverse loop due to possible Processor disposal.
            for (int i = this.instances.Count; i-- > 0;) {
                this.instances[i].Update();
            }

            foreach (ISystem subsystem in this.subsystems) {
                subsystem.Update();
            }
        }

        public virtual void FixedUpdate() {
            // Reverse loop due to possible Processor disposal.
            for (int i = this.instances.Count; i-- > 0;) {
                this.instances[i].FixedUpdate();
            }

            foreach (ISystem subsystem in this.subsystems) {
                subsystem.FixedUpdate();
            }
        }

        public virtual void LateUpdate() {
            // Reverse loop due to possible Processor disposal.
            for (int i = this.instances.Count; i-- > 0;) {
                this.instances[i].LateUpdate();
            }

            foreach (ISystem subsystem in this.subsystems) {
                subsystem.LateUpdate();
            }
        }

        protected List<TSystem> GetSubsystemInstances<TSystem>()
        where TSystem : ISystem {
            List<TSystem> converted = new List<TSystem>();

            foreach (ISystem subsystem in this.subsystems) {
                TSystem iteration;

                try {
                    iteration = (TSystem)subsystem;
                } catch {
                    continue;
                } 

                if (iteration == null)
                    continue;

                converted.Add(iteration);
            }

            return converted;
        }
    }

    [Serializable]
    public abstract class Subsystem<TProcessor, TProfile> : ISystem
    where TProcessor : Processor
    where TProfile : Profile {

        [SerializeField]
        private bool deployOnStartup;
        [SerializeField, Tooltip("Whether to automatically mark this System as ready during MonoBehaviour.Awake.\n\n" +
                                 "If false it is required to manually set this System as ready, or its instances will " +
                                 "not have Processor.Start invoked.")]
        private bool readyOnStartup = true;
        private bool isReady;
        [SerializeField]
        private GameObject instancePrefab;
        [SerializeField]
        private readonly ScriptableObject constants;
        private ISystem parent;
        protected readonly List<TProcessor> instances = new List<TProcessor>();

        Processor ISystem.DeployInstance => DeployInstance();

        public bool IsReady => this.isReady;
        public string NewInstanceName => $"{GetType().Name} instance #{this.instances.Count + 1}";
        public Transform Transform { get; set; }
        public GameObject InstancePrefab => this.instancePrefab;
        public List<TProcessor> Instances => this.instances;
        public ScriptableObject Constants => this.constants;
        public ISystem Parent => this.parent;

        public event EventHandler<Type> InstanceDeployed;
        public event EventHandler<Type> InstanceRemoved;
        public event EventHandler Ready;

        protected Subsystem() { }

        /// <summary>
        /// Since unity 2019 does not serialise generics, it is needed to provide a wrapper type in the class that inherits from this one.
        /// </summary>
        protected abstract void UpdateSerializableInstances(object sender, Type instanceType);

        /// <summary>
        /// Subsystems are initialised through the Awake method as they are serialized.
        /// </summary>
        public virtual void Awake(Transform transform, ISystem parent) {
            this.Transform = transform;
            this.parent = parent;

            InstanceDeployed += UpdateSerializableInstances;
            InstanceRemoved += UpdateSerializableInstances;

            if (this.deployOnStartup && this.instances.Count == 0)
                DeployInstance();

            if (this.readyOnStartup)
                StartReadyCheck();
        }

        private void StartReadyCheck() {
            if (this.parent.IsReady) {
                SetReady();
                return;
            }

            this.parent.Ready += (sender, args) => { SetReady(); };
        }

        protected void SetReady() {
            this.isReady = true;
            Ready?.Invoke(this, null);

            foreach (TProcessor processor in this.instances) {
                processor.Start();
            }
        }

        public TProcessor DeployInstance(Type processorType, Type profileType, GameObject instancePrefab = null) {
            if (Transform == null)
                throw new InvalidOperationException($"Attempted to Deploy an instance of Subsystem {GetType().Name} before it has been initialised.");

            TProcessor processor = (TProcessor)System.DeployInstance(processorType, profileType, GetType(), this, instancePrefab, Transform, NewInstanceName);
            this.instances.Add(processor);

            if (this.isReady)
                processor.Start();

            InstanceDeployed?.Invoke(processor, GetType());

            return processor;
        }

        public TProcessor DeployInstance() {
            return DeployInstance(typeof(TProcessor), typeof(TProfile), this.instancePrefab);
        }

        public void RemoveInstance(Processor processor) {
            this.instances.Remove((TProcessor)processor);
            InstanceRemoved?.Invoke(processor, GetType());
        }

        public virtual void Update() {
            // Reverse loop due to possible Processor disposal.
            for (int i = this.instances.Count; i-- > 0;) {
                this.instances[i].Update();
            }
        }

        public virtual void FixedUpdate() {
            // Reverse loop due to possible Processor disposal.
            for (int i = this.instances.Count; i-- > 0;) {
                this.instances[i].FixedUpdate();
            }
        }

        public virtual void LateUpdate() {
            // Reverse loop due to possible Processor disposal.
            for (int i = this.instances.Count; i-- > 0;) {
                this.instances[i].LateUpdate();
            }
        }
    }
}