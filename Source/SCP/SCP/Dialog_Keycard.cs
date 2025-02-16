﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;


namespace SCP
{
    public class Dialog_Keycard : Window
    {
        public Dialog_Keycard(Func<int, string> textGetter, int from, int to, Action<int> confirmAction, int startingValue = -2147483648)
        {
            this.textGetter = textGetter;
            this.from = from;
            this.to = to;
            this.confirmAction = confirmAction;
            this.forcePause = true;
            this.closeOnClickedOutside = true;
            this.curValue = startingValue == -2147483648 ? from : startingValue;
        }
        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(300f, 130f);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect rect = new Rect(inRect.x, inRect.y + 15f, inRect.width, 30f);
            this.curValue = (int)Widgets.HorizontalSlider(rect, (float)this.curValue, (float)this.from, (float)this.to,
                true, this.textGetter(this.curValue), null, null, 1f);
            Text.Font = GameFont.Small;
            Rect rect2 = new Rect(inRect.x, inRect.yMax - 30f, inRect.width / 2f, 30f);
            if (Widgets.ButtonText(rect2, "CancelButton".Translate(), true, false, true))
            {
                this.Close(true);
            }
            Rect rect3 = new Rect(inRect.x + inRect.width / 2f, inRect.yMax - 30f, inRect.width / 2f, 30f);
            if (Widgets.ButtonText(rect3, "OK".Translate(), true, false, true))
            {
                this.Close(true);
                this.confirmAction(this.curValue);
            }
        }

        public Func<int, string> textGetter;

        public int from;

        public int to;

        private Action<int> confirmAction;

        private int curValue;

        private const float BotAreaHeight = 30f;

        private const float TopPadding = 15;

    }
}
