using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HotbarTimers
{
    public unsafe class UIHelper
    {
        public static T* CleanAlloc<T>() where T : unmanaged
        {
            return (T*)CleanAlloc((ulong)sizeof(T));
        }

        public static void* CleanAlloc(ulong size)
        {
            var alloc = Alloc(size);
            IMemorySpace.Memset(alloc, 0, size);
            return alloc;
        }

        public static void* Alloc(ulong size)
        {
            return IMemorySpace.GetUISpace()->Malloc(size, 8);
        }

        public static void Show(AtkTextNode* node) => Show((AtkResNode*)node);
        public static void Show(AtkImageNode* node) => Show((AtkResNode*)node);
        public static void Show(AtkResNode* node)
        {
            node->Flags |= 0x10;
            node->Flags_2 |= 0x1;
        }

        public static void Hide(AtkTextNode* node) => Hide((AtkResNode*)node);
        public static void Hide(AtkImageNode* node) => Hide((AtkResNode*)node);
        public static void Hide(AtkResNode* node)
        {
            node->Flags &= ~0x10;
            node->Flags_2 |= 0x1;
        }

        public static void Link(AtkResNode* next, AtkResNode* prev)
        {
            if (next == null || prev == null) return;
            next->PrevSiblingNode = prev;
            prev->NextSiblingNode = next;
        }
    }
}
