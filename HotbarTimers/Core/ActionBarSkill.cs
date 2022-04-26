using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Numerics;

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
        
        private bool Visible = false;        
        private uint NodeIdx = 200;

        public ActionBarSkill(ActionBarSlot* actionBarSlot, AtkComponentNode* iconComponent, 
            string name, int actionBarIndex, int slotIndex, Configuration configuration)
        {
            ActionBarSlot = actionBarSlot;
            IconComponent = iconComponent;
            Name = name;
            ActionBarIndex = actionBarIndex;
            SlotIndex = slotIndex;

            Initialize(configuration);
        }

        private void Initialize(Configuration configuration)
        {
            NodeList = IconComponent->Component->UldManager.NodeList;
            OriginalCdText = (AtkTextNode*)NodeList[13];

            Combo = CreateComboNode();
            DurationText = CreateTextNode(0, 0, AlignmentType.Center, 
                configuration.StatusTimerFontSize, GeneralUtils.CalculateByteColorFromVector(configuration.StatusTimerFontColor));
            StackText = CreateTextNode(-3, 5, AlignmentType.TopRight, 
                configuration.StackCountFontSize, GeneralUtils.CalculateByteColorFromVector(configuration.StackCountFontColor));

            var originalOverlay = NodeList[1];
            UIHelper.Link(originalOverlay, (AtkResNode*)Combo);
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

        private AtkTextNode* CreateTextNode(float x, float y, AlignmentType alignmentType, int fontSize, ByteColor fontColor)
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
            text->AlignmentFontType = (byte)alignmentType;
            text->FontSize = (byte)fontSize;
            text->TextFlags = OriginalCdText->TextFlags;
            text->TextColor = fontColor;
            text->EdgeColor = new ByteColor { R = 0, G = 0, B = 0, A = 255 };
            text->AtkResNode.ParentNode = rootNode;

            return text;
        }

        public void Show(float remainingTime, int stackCount)
        {
            if(remainingTime >= 0.0)
            {
                string format = "0";
                if (remainingTime < 3.0) format = "0.0";
                DurationText->SetText(remainingTime.ToString(format));

                UIHelper.Show(DurationText);
                UIHelper.Show(Combo);
                UIHelper.Hide(OriginalCdText);

                if (stackCount > 0)
                {
                    StackText->SetText(stackCount.ToString());
                    UIHelper.Show(StackText);
                }

                Visible = true;
            }
        }

        public void Hide(bool force = false)
        {
            if (Visible || force)
            {
                UIHelper.Show(OriginalCdText);
                UIHelper.Hide(Combo);
                UIHelper.Hide(DurationText);
                UIHelper.Hide(StackText);
                Visible = false;
            }
        }

        public override string ToString() => $"{Name}; Action Bar: {ActionBarIndex}; Slot: {SlotIndex}";

        public void Dispose()
        {
            UIHelper.Dispose(Combo);
            UIHelper.Dispose((AtkResNode*)DurationText);
            UIHelper.Dispose((AtkResNode*)StackText);
            GC.SuppressFinalize(this);
        }
    }
}
