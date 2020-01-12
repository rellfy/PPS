#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PPS;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Assembly = System.Reflection.Assembly;
using Object = UnityEngine.Object;

/// <summary>
/// TODO: Open editor window for new system name input before generating the files.
/// </summary>
public static class SystemScriptFactory {

    private static string CurrentPath => AssetDatabase.GetAssetPath(Selection.activeObject);

    private struct Boilerplate {
        public string processor;
        public string profile;
        public string system;
        public Type type;
    }

    private enum Type {
        System,
        Subsystem
    }

    private static Boilerplate GenerateBoilerplate(Type type) {
        string systemName = $"NewS{(type == Type.Subsystem ? "ubs" : "")}ystem";

        Boilerplate boilerplate = new Boilerplate {
            type = type,
            system = type == Type.Subsystem
                ? 
                "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using UnityEngine;\n" +
                "using PPS;\n" +
                "\n" +
                "namespace SomeNamespace {\n" +
                "\n" +
                "    [Serializable]\n" +
                "    public class NewSubsystem : PPS.Subsystem<NewProcessor> {\n" +
                "\n" +
                "        /// <summary>\n" +
                "        /// Serializable instance list.\n" +
                "        /// </summary>\n" +
                "        [SerializeField]\n" +
                "        private List<NewProcessor> newProcessorList;\n" +
                "\n" +
                "        /// <summary>\n" +
                "        /// Subsystems are serialized, therefore they are initialised through Awake.\n" +
                "        /// </summary>\n" +
                "        public override void Awake(Transform transform, ISystem parent) {\n" +
                "            base.Awake(transform, parent);\n" +
                "        }\n" +
                "\n" +
                "        /// <summary>\n" +
                "        /// Unity 2019 does not serialize generics. For that reason, we convert the generic\n" +
                "        /// to the specific type that we want to serialize here.\n" +
                "        /// </summary>\n" +
                "        protected override void UpdateSerializableInstances(object sender, Type instanceType) {\n" +
                "            this.newProcessorList = this.instances;\n" +
                "        }\n" +
                "    }\n" +
                "}"
                :
                "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using UnityEngine;\n" +
                "using PPS;\n" +
                "\n" +
                "namespace SomeNamespace { \n" +
                "\n" +
                "    [Serializable]\n" +
                "    public class NewSystem : System<NewProcessor> {\n" +
                "\n" +
                "        /// <summary>\n" +
                "        /// Serializable instance list.\n" +
                "        /// </summary>\n" +
                "        [SerializeField]\n" +
                "        private List<NewProcessor> newProcessorList;\n" +
                "\n" +
                "        public override void Awake() {\n" +
                "            base.Awake();\n" +
                "        }\n" +
                "\n" +
                "        /// <summary>\n" +
                "        /// Unity 2019 does not serialize generics. For that reason, we convert the generic\n" +
                "        /// to the specific type that we want to serialize here.\n" +
                "        /// </summary>\n" +
                "        protected override void UpdateSerializableInstances(object sender, Type instanceType) {\n " +
                "           this.newProcessorList = this.instances;\n" +
                "        }\n" +
                "    }\n" +
                "}",
            processor = 
                "using System;\n" +
                "using UnityEngine;\n" +
                "using PPS;\n" +
                "\n" +
                "namespace SomeNamespace { \n" +
                "\n" +
                "    [Serializable]\n" +
                "    public class NewProcessor : Processor<" +systemName+"> {\n" +
                "\n" +
                "        [SerializeField]\n" +
                "        private NewProfile profile;\n" +
                "\n" +
                "        public NewProcessor(" + systemName+" system, GameObject instance) : base(system, instance) {\n" +
                "            this.profile = new NewProfile(GameObject);\n" +
                "        }\n" +
                "    }\n" +
                "}",
            profile =
                "using System;\n" + 
                "using UnityEngine;\n" +
                "using PPS;\n" +
                "\n" +
                "namespace SomeNamespace { \n" +
                "\n" +
                "    [Serializable]\n" +
                "    public class NewProfile : Profile {\n" +
                "\n" +
                "        public NewProfile(GameObject gameObject) : base(gameObject) { }\n" +
                "    }\n" +
                "}"
        };

        return boilerplate;
    }

    [MenuItem("Assets/Create/PPS/New System", false, 0)]
    private static void NewSystem() {
        InitialiseSystemBoilerplate(Type.System);
    }

    [MenuItem("Assets/Create/PPS/New Subsystem", false, 0)]
    private static void NewSubsystem() {
        InitialiseSystemBoilerplate(Type.Subsystem);
    }

    private static void InitialiseSystemBoilerplate(Type type) {
        if (string.IsNullOrEmpty(CurrentPath))
            throw new Exception("Please generate a new system via the project view's context menu through \"Assets/Create/PPS\"");

        CreateFolders();
        CreateScripts(GenerateBoilerplate(type));
        AssetDatabase.Refresh();
    }

    private static void CreateFolders() {
        Directory.CreateDirectory(GetFilePath("Processors"));
        Directory.CreateDirectory(GetFilePath("Profiles"));
    }

    private static void CreateScripts(Boilerplate boilerplate) {
        File.WriteAllText(GetFilePath($"NewS{(boilerplate.type == Type.Subsystem ? "ubs" : "")}ystem.cs"), boilerplate.system);
        File.WriteAllText(GetFilePath($"Processors/NewProcessor.cs"),boilerplate.processor);
        File.WriteAllText(GetFilePath($"Profiles/NewProfile.cs"), boilerplate.profile);
    }

    private static string GetFilePath(string relative) {
        return Path.GetFullPath(Path.Combine(CurrentPath, relative));
    }
}
#endif
