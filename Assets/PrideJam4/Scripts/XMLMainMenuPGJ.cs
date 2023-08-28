using System;
using System.Collections.Generic;
using System.Linq;
using BagoumLib;
using BagoumLib.Cancellation;
using BagoumLib.Culture;
using BagoumLib.Transitions;
using Danmokou.Behavior;
using Danmokou.Core;
using Danmokou.Danmaku;
using Danmokou.DMath;
using Danmokou.GameInstance;
using Danmokou.Graphics.Backgrounds;
using Danmokou.Player;
using Danmokou.Scriptables;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.Scripting;
using static Danmokou.Services.GameManagement;
using static Danmokou.UI.XML.XMLUtils;
using static Danmokou.Core.LocalizedStrings.UI;
using static Danmokou.UI.PlayModeCommentator;
using Danmokou.Services;

namespace Danmokou.UI.XML
{
    [Preserve]
    public class XMLMainMenuPGJ : XMLMainMenu
    {
        private static List<CacheInstruction>? _returnTo;
        protected override List<CacheInstruction>? ReturnTo
        {
            get => _returnTo;
            set => _returnTo = value;
        }

        private UIScreen OptionsScreen = null!;
        private UIScreen ReplayScreen = null!;
        private UIScreen RecordsScreen = null!;
        private UIScreen StatsScreen = null!;
        private UIScreen MusicRoomScreen = null!;
        private UIScreen GameDetailsScreen = null!;
        private UIScreen? AchievementsScreen;
        private UIScreen PlayerDataScreen = null!;
        private UIScreen LicenseScreen = null!;

        protected override UIScreen?[] Screens => new[] {
        MainScreen,
        OptionsScreen, ReplayScreen, RecordsScreen,
        StatsScreen, AchievementsScreen, MusicRoomScreen, GameDetailsScreen, PlayerDataScreen,
        LicenseScreen
        };

        public VisualTreeAsset AchievementsNodeV = null!;

        private static UINode[] DifficultyNodes(Func<FixedDifficulty, UINode> map) =>
            GameManagement.VisibleDifficulties.Select(map).ToArray();

        private static UINode[] DifficultyFuncNodes(Func<FixedDifficulty, Action> map) =>
            DifficultyNodes(d => new FuncNode(d.Describe(), map(d)));

        public override void FirstFrame()
        {
            var game = References.CampaignGameDef;
            FixedDifficulty dfc = FixedDifficulty.Normal;
            var sakuya = game.Campaign.players[0];
            var tojiko = game.Campaign.players[1];
            ShipConfig[] shipConfigs = new ShipConfig[2];
            shipConfigs[0] = sakuya;
            shipConfigs[1] = tojiko;

            var sakuyaShot = sakuya.shots2[0];
            var tojikoShot = tojiko.shots2[0];
            ShotConfig[] shotConfigs = new ShotConfig[2];
            shotConfigs[0] = sakuyaShot.shot;
            shotConfigs[1] = tojikoShot.shot;

            var defaultSupport = sakuya.supports[0];
            var shipshots = shipConfigs.Zip(shotConfigs, (x, y) => (x, y)).ToArray();

            TeamConfig Team() => new(0, Subshot.TYPE_D, defaultSupport.ability, shipshots);
            //TODO I currently don't have a story around game-specific configurations of meter/etc,
            // this disabling is a stopgap measure until then.
            SharedInstanceMetadata Meta() => new(Team(), new DifficultySettings(dfc) { meterEnabled = true });

            OptionsScreen = this.OptionsScreen(true);
            GameDetailsScreen = new UIScreen(this, "GAME DETAILS") { Builder = XMLHelpers.GameResultsScreenBuilder };
            PlayerDataScreen = this.AllPlayerDataScreens(game, GameDetailsScreen, out ReplayScreen, out StatsScreen,
                out AchievementsScreen, out RecordsScreen, AchievementsNodeV);
            LicenseScreen = this.LicenseScreen(References.licenses);

            MainScreen = new UIScreen(this, null, UIScreen.Display.Unlined)
            {
                Builder = (s, ve) =>
                {
                    s.Margin.SetLRMargin(720, null);
                    var c = ve.AddColumn();
                    c.style.maxWidth = 40f.Percent();
                    c.style.paddingTop = 500;
                },
                SceneObjects = MainScreenOnlyObjects}.WithBG(PrimaryBGConfig);

            foreach(var s in Screens)
            {
                if(s != MainScreen)
                {
                    s?.WithBG(SecondaryBGConfig);
                }    
            }

            _ = new UIColumn(MainScreen, null,
                new FuncNode(main_gamestart, () => InstanceRequest.RunCampaign(MainCampaign, null, Meta()))
                    .With(large1Class),
                new TransferNode(main_playerdata, PlayerDataScreen),
                new TransferNode(main_options, OptionsScreen)
                    .With(large1Class),
                new FuncNode(main_quit, Application.Quit)
                    .With(large1Class));;

            bool doAnim = ReturnTo == null;
            base.FirstFrame();
            if (doAnim)
            {
                //_ = TransitionHelpers.TweenTo(720f, 0f, 1f, x => UIRoot.style.left = x, M.EOutSine).Run(this);
                _ = TransitionHelpers.TweenTo(0f, 1f, 0.8f, x => UIRoot.style.opacity = x, x => x).Run(this);
            }
        }
    }
}