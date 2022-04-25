using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Diagnostics;

namespace TimersOnSkills
{
    public unsafe class ActionBarSkill
    {
        public ActionBarSlot* ActionBarSlot { get; init; }
        public AtkComponentNode* IconComponent { get; init; }
        public string Name { get; init; }
        public int ActionBarIndex { get; init; }
        public int SlotIndex { get; init; }



        private AtkImageNode* Combo;
        private AtkTextNode* Text;
        private AtkResNode* OriginalOverlay;
        private AtkResNode* CdText;
        private AtkResNode* StackText;
        private bool Visible = false;
        private AtkResNode** NodeList;

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
            var nodeList = IconComponent->Component->UldManager.NodeList;
            NodeList = nodeList;
            OriginalOverlay = nodeList[1];
            CdText = nodeList[13];
            StackText = nodeList[11];

            var originalBorder = (AtkImageNode*)nodeList[4];
            var rootNode = (AtkResNode*)IconComponent;
            uint nodeIdx = 200;
            Combo = UIHelper.CleanAlloc<AtkImageNode>();
            Combo->Ctor();
            Combo->AtkResNode.NodeID = nodeIdx + 1;
            Combo->AtkResNode.Type = NodeType.Image;
            Combo->AtkResNode.X = -14;
            Combo->AtkResNode.Y = -11;
            Combo->AtkResNode.Width = 48;
            Combo->AtkResNode.Height = 48;
            Combo->AtkResNode.Flags = 8243;
            Combo->AtkResNode.Flags_2 = 1;
            Combo->AtkResNode.Flags_2 |= 4;
            Combo->WrapMode = 0;
            Combo->PartId = (ushort)16;
            Combo->PartsList = originalBorder->PartsList;
            Combo->AtkResNode.ParentNode = rootNode;

            Text = UIHelper.CleanAlloc<AtkTextNode>();
            Text->Ctor();
            Text->AtkResNode.NodeID = nodeIdx + 2;
            Text->AtkResNode.Type = NodeType.Text;
            Text->AtkResNode.X = 2;
            Text->AtkResNode.Y = 3;
            Text->AtkResNode.Width = 40;
            Text->AtkResNode.Height = 40;
            Text->LineSpacing = 40;
            Text->AlignmentFontType = 20;
            Text->FontSize = 16;
            Text->TextFlags = 16;
            Text->TextColor = new ByteColor { R = 255, G = 255, B = 255, A = 255 };
            Text->EdgeColor = new ByteColor { R = 51, G = 51, B = 51, A = 255 };
            Text->AtkResNode.ParentNode = rootNode;

            UIHelper.Link(OriginalOverlay, (AtkResNode*)Combo);
            UIHelper.Link((AtkResNode*)Combo, (AtkResNode*)Text);

            IconComponent->Component->UldManager.UpdateDrawNodeList();

            Hide(true);
        }

        public void Show(float remainingTime)
        {
            if (remainingTime >= 0.0)
            {
                string format = "0";
                if (remainingTime < 1.0) format = "0.0";
                Text->SetText(remainingTime.ToString(format));
            }
                
            UIHelper.Show((AtkResNode*)Combo);
            UIHelper.Show((AtkResNode*)Text);
            UIHelper.Hide(CdText);
            Visible = true;
        }

        public void Hide(bool force = false)
        {
            if (Visible || force)
            {
                Text->SetText("");

                UIHelper.Show(CdText);
                UIHelper.Hide((AtkResNode*)Combo);
                UIHelper.Hide((AtkResNode*)Text);
                Visible = false;
            }
        }

        public override string ToString()
        {
            return $"{Name}; Action Bar: {ActionBarIndex}; Slot: {SlotIndex}";
        }
    }
}
