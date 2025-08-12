namespace JHelper.UnityManagers.SceneManager;

public partial class SceneManager
{
    internal readonly struct SceneManagerOffsets
    {
        internal readonly byte sceneCount;
        internal readonly byte activeScene;
        internal readonly byte dontDestroyOnLoadScene;
        internal readonly byte assetPath;
        internal readonly byte buildIndex;
        internal readonly byte rootStorageContainer;
        internal readonly byte gameObject;
        internal readonly byte gameObjectName;
        internal readonly byte klass;
        internal readonly byte klassName;
        internal readonly byte childrenPointer;

        internal SceneManagerOffsets(bool is64bit)
        {
            if (is64bit)
            {
                sceneCount = 0x18;
                activeScene = 0x48;
                dontDestroyOnLoadScene = 0x70;
                assetPath = 0x10;
                buildIndex = 0x98;
                rootStorageContainer = 0xB0;
                gameObject = 0x30;
                gameObjectName = 0x60;
                klass = 0x28;
                klassName = 0x48;
                childrenPointer = 0x70;
            }
            else
            {
                sceneCount = 0x10;
                activeScene = 0x28;
                dontDestroyOnLoadScene = 0x40;
                assetPath = 0xC;
                buildIndex = 0x70;
                rootStorageContainer = 0x88;
                gameObject = 0x1C;
                gameObjectName = 0x3C;
                klass = 0x18;
                klassName = 0x2C;
                childrenPointer = 0x50;
            }
        }
    }
}