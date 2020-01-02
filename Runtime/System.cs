using System;
using System.Collections.Generic;
using UnityEngine;

namespace PPS {

    public interface ISystem {

        void Update();
        void FixedUpdate();
        void RemoveInstance(Processor processor);
        Processor DeployInstance { get; }
        event EventHandler<Type> InstanceDeployed;
        event EventHandler<Type> InstanceRemoved;
    }

    internal static class System {

        public static TProcessor DeployInstance<TProcessor, TProfile>(Type systemType, ISystem system, GameObject prefab, Transform parent, string instanceName)
        where TProcessor : Processor
        where TProfile : Profile {
            GameObject instance = prefab != null ? UnityEngine.Object.Instantiate(prefab, parent) : new GameObject();
            instance.name = instanceName;

            Type[] profileConstructorTypes = { typeof(GameObject) };
            object[] profileConstructorParams = { instance };
            TProfile profile = typeof(TProfile).GetConstructor(profileConstructorTypes)?.Invoke(profileConstructorParams) as TProfile;

            Type[] processorConstructorTypes = { systemType, typeof(TProfile) };
            object[] processorConstructorParams = { system, profile };
            TProcessor processor = typeof(TProcessor).GetConstructor(processorConstructorTypes)?.Invoke(processorConstructorParams) as TProcessor;

            if (processor == null || profile == null)
                throw new Exception("Could not instantiate the instance's Processor or Profile from type.");

            return processor;
        }
    }

    [Serializable]
    public abstract class System<TProcessor, TProfile> : MonoBehaviour, ISystem
    where TProcessor : Processor
    where TProfile : Profile {

        [SerializeField]
        private bool deployOnStartup;
        [SerializeField]
        private GameObject instancePrefab;
        [SerializeField]
        private ScriptableObject constants;
        protected List<TProcessor> instances = new List<TProcessor>();
        private List<ISystem> subsystems = new List<ISystem>();

        public ScriptableObject Constants => this.constants;
        Processor ISystem.DeployInstance => DeployInstance();

        public event EventHandler<Type> InstanceDeployed;
        public event EventHandler<Type> InstanceRemoved;

        public virtual void Awake() {
            InstanceDeployed += UpdateSerializableInstances;
            InstanceRemoved += UpdateSerializableInstances;

            DeploySubsystems();

            if (this.deployOnStartup && this.instances.Count == 0)
                DeployInstance();
        }

        /// <summary>
        /// Since unity 2019 does not serialise generics, it is needed to provide a wrapper type in the class that inherits from this one.
        /// </summary>
        protected abstract void UpdateSerializableInstances(object sender, Type instanceType);

        protected virtual void DeploySubsystems() { }

        public TProcessor DeployInstance() {
            string instanceName = $"{GetType().Name} instance #{this.instances.Count + 1}";
            TProcessor processor = System.DeployInstance<TProcessor, TProfile>(GetType(), this, this.instancePrefab, transform, instanceName);
            this.instances.Add(processor);
            InstanceDeployed?.Invoke(this, null);

            return processor;
        }

        public void RemoveInstance(Processor processor) {
            this.instances.Remove((TProcessor)processor);
            InstanceRemoved?.Invoke(this, null);
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

            subsystem.transform = new GameObject(subsystem.GetType().Name).transform;
            subsystem.transform.parent = transform;
            this.subsystems.Add(subsystem);
            subsystem.Awake(subsystem.transform, this);
        }

        public virtual void Update() {
            foreach (TProcessor instance in this.instances) {
                instance.Update();
            }

            foreach (ISystem subsystem in this.subsystems) {
                subsystem.Update();
            }
        }

        public virtual void FixedUpdate() {
            foreach (TProcessor instance in this.instances) {
                instance.FixedUpdate();
            }

            foreach (ISystem subsystem in this.subsystems) {
                subsystem.FixedUpdate();
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
        [SerializeField]
        private GameObject instancePrefab;
        [SerializeField]
        private readonly ScriptableObject constants;
        private ISystem parent;
        protected readonly List<TProcessor> instances = new List<TProcessor>();

        public Transform transform { get; set; }
        public List<TProcessor> Instances => this.instances;
        public ScriptableObject Constants => this.constants;
        public ISystem Parent => this.parent;
        Processor ISystem.DeployInstance => DeployInstance();


        public event EventHandler<Type> InstanceDeployed;
        public event EventHandler<Type> InstanceRemoved;

        protected Subsystem() { }

        /// <summary>
        /// Since unity 2019 does not serialise generics, it is needed to provide a wrapper type in the class that inherits from this one.
        /// </summary>
        protected abstract void UpdateSerializableInstances(object sender, Type instanceType);

        /// <summary>
        /// Subsystems are initialised through the Awake method as they are serialized.
        /// </summary>
        /// <param name="transform"></param>
        public virtual void Awake(Transform transform, ISystem parent) {
            this.transform = transform;
            this.parent = parent;

            InstanceDeployed += UpdateSerializableInstances;
            InstanceRemoved += UpdateSerializableInstances;

            if (this.deployOnStartup && this.instances.Count == 0)
                DeployInstance();
        }

        public TProcessor DeployInstance() {
            if (this.transform == null)
                throw new InvalidOperationException($"Attempted to Deploy an instance of Subsystem {GetType().Name} before it has been initialised.");

            string instanceName = $"{GetType().Name} instance #{this.instances.Count + 1}";
            TProcessor processor = System.DeployInstance<TProcessor, TProfile>(GetType(), this, this.instancePrefab, transform, instanceName);
            this.instances.Add(processor);
            InstanceDeployed?.Invoke(this, null);

            return processor;
        }

        public void RemoveInstance(Processor processor) {
            this.instances.Remove((TProcessor)processor);
            InstanceRemoved?.Invoke(this, null);
        }

        public virtual void Update() {
            foreach (TProcessor instance in this.instances) {
                instance.Update();
            }
        }

        public virtual void FixedUpdate() {
            foreach (TProcessor instance in this.instances) {
                instance.FixedUpdate();
            }
        }
    }
}