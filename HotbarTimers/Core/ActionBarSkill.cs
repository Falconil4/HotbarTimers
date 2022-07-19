using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Diagnostics;

namespace HotbarTimers
{
    public unsafe class ActionBarSkill : IDisposable
    {
        public ActionBarSlot* ActionBarSlot { get; init; }
        public AtkComponentNode* IconComponent { get; init; }
        public string Name { get; init; }
        public int ActionBarIndex { get; init; }
        public int SlotIndex { get; init; }

        private AtkResNode** NodeList;
        private AtkImageNode* Combo;
        private AtkTextNode* DurationText;
        private AtkTextNode* StackText;
        private AtkTextNode* OriginalCdText;
        private AtkResNode* OriginalOverlay;

        private bool Visible = false;        
        private static uint NodeIdx = 200;

        public ActionBarSkill(ActionBarSlot* actionBarSlot, AtkComponentNode* iconComponent, 
            string name, int actionBarIndex, int slotIndex)
        {
            ActionBarSlot = actionBarSlot;
            IconComponent = iconComponent;
            Name = name;
            ActionBarIndex = actionBarIndex;
            SlotIndex = slotIndex;

            Initialize();
        }

        private void Initialize()
        {
            if (HotbarTimers.Configuration == null) return;

            NodeList = IconComponent->Component->UldManager.NodeList;
            OriginalCdText = (AtkTextNode*)NodeList[13];
            OriginalOverlay = NodeList[1];

            Combo = CreateComboNode();
            DurationText = CreateTextNode(0, 0, AlignmentType.Center, HotbarTimers.Configuration.StatusTimerTextConfig);
            StackText = CreateTextNode(-3, 5, AlignmentType.TopRight, HotbarTimers.Configuration.StackCountTextConfig);

            UIHelper.Link(OriginalOverlay, (AtkResNode*)Combo);
            UIHelper.Link((AtkResNode*)Combo, (AtkResNode*)DurationText);
            UIHelper.Link((AtkResNode*)DurationText, (AtkResNode*)StackText);

            IconComponent->Component->UldManager.UpdateDrawNodeList();

            Hide(true);
        }

        private AtkImageNode* CreateComboNode()
        {
            var originalBorder = (AtkImageNode*)NodeList[4];
            var rootNode = (AtkResNode*)IconComponent;
            
            var combo = UIHelper.CleanAlloc<AtkImageNode>();
            combo->Ctor();
            combo->AtkResNode.NodeID = NodeIdx++;
            combo->AtkResNode.Type = NodeType.Image;
            combo->AtkResNode.X = -14;
            combo->AtkResNode.Y = -11;
            combo->AtkResNode.Width = rootNode->Width;
            combo->AtkResNode.Height = rootNode->Height;
            combo->AtkResNode.Flags = 8243;
            combo->AtkResNode.Flags_2 = 1;
            combo->AtkResNode.Flags_2 |= 4;
            combo->WrapMode = 0;
            combo->PartId = (ushort)16;
            combo->PartsList = originalBorder->PartsList;
            combo->AtkResNode.ParentNode = rootNode;

            return combo;
        }

        private AtkTextNode* CreateTextNode(float x, float y, AlignmentType alignmentType, TextConfig config)
        {
            var rootNode = (AtkResNode*)IconComponent;

            var text = UIHelper.CleanAlloc<AtkTextNode>();
            text->Ctor();
            text->AtkResNode.NodeID = NodeIdx++;
            text->AtkResNode.Type = NodeType.Text;

            text->AtkResNode.X = x;
            text->AtkResNode.Y = y;
            text->AtkResNode.Width = rootNode->Width;
            text->AtkResNode.Height = rootNode->Height;
            text->LineSpacing = OriginalCdText->LineSpacing;
            text->AlignmentFontType = (byte)((0x10 * (byte)config.FontType) | (byte)alignmentType);
            text->FontSize = (byte)config.FontSize;
            text->TextFlags = OriginalCdText->TextFlags;
            text->TextColor = GeneralUtils.CalculateByteColorFromVector(config.FontColor);
            text->EdgeColor = new ByteColor { R = 0, G = 0, B = 0, A = 255 };
            text->AtkResNode.ParentNode = rootNode;
            
            return text;
        }

        public void Show(float remainingTime, int stackCount)
        {
            if (remainingTime < 0.0) remainingTime *= -1;

            string format = "0";
            if (remainingTime < 3.0) format = "0.0";
            DurationText->SetText(remainingTime.ToString(format));

            UIHelper.Show(DurationText);
            UIHelper.Show(Combo);
            UIHelper.Hide(OriginalCdText);

            if (stackCount > 0 && stackCount < 100)
            {
                StackText->SetText(stackCount.ToString());
                UIHelper.Show(StackText);
            }

            Visible = true;
        }

        public void Hide(bool force = false)
        {
            if (Visible || force)
            {
                UIHelper.Show(OriginalCdText);
                UIHelper.Hide(Combo);
                UIHelper.Hide(DurationText);
                UIHelper.Hide(StackText);

                DurationText->SetText("");
                StackText->SetText("");
                
                Visible = false;
            }
        }

        public override string ToString() => $"{Name}; Action Bar: {ActionBarIndex}; Slot: {SlotIndex}";

        public void Dispose()
        {
            UIHelper.Show(OriginalOverlay);
            UIHelper.Show(OriginalCdText);

            UIHelper.Unlink(StackText);
            UIHelper.Unlink(DurationText);
            UIHelper.Unlink(Combo);
            
            IconComponent->Component->UldManager.UpdateDrawNodeList();

            UIHelper.Destroy(Combo);
            UIHelper.Destroy(DurationText);
            UIHelper.Destroy(StackText);

            GC.SuppressFinalize(this);
        }

        ~ActionBarSkill() => Dispose();
    }
}
