public static class SoundCueNames
{
    public static class UI
    {
        public const string Confirm = "UiConfirm";
        public const string CursorMove = "UiCursorMove";
        public const string SkillSelectAppear = "SkillSelectAppear";
    }

    public static class Common
    {
        public const string ArmorBreak = "ArmorBreak";
        public const string EnemyFinisher = "EnemyFinisher";
        public const string ItemHealPickup = "ItemHealPickup";
    }

    public static class Player
    {
        public const string WeaponSwingWarrior = "PlayerWeaponSwingWarrior";
        public const string WeaponSwingThunder = "PlayerWeaponSwingThunder";
        public const string HitEnemyWarrior = "PlayerHitEnemyWarrior";
        public const string HitEnemyThunder = "PlayerHitEnemyThunder";
        public const string HitArmorWarrior = "PlayerHitArmorWarrior";
        public const string HitArmorThunder = "PlayerHitArmorThunder";
        public const string ThunderElectrify = "PlayerThunderElectrify";
        public const string Damage = "PlayerDamage";
        public const string ModeChangeActivate = "ModeChange_Activate";
        public const string ModeChangeDeactivate = "ModeChange_Deactivate";
        public const string WarriorChargeReady = "WarriorChargeReady";
        public const string Footstep = "PlayerFootstep";
        public const string ThunderDodge = "PlayerThunderDodge";
        public const string WarriorRoll = "PlayerRoll";
        public const string LightningStrike = "LightningStrike";
        public const string GroundCrush = "GroundCrush";
    }

    public static class Skill
    {
        public const string LightningStrike = Player.LightningStrike;
        public const string GroundCrush = Player.GroundCrush;
        public const string ElectricDodgeSkill = "ElectricDodgeSkill";
    }

    public static class Enemy
    {
        public const string DraugrWeaponSwing = "DraugrWeaponSwing";
        public const string DraugrShieldbash = "DraugrShieldbash";
        public const string StoneRingAttack = "StoneRingAttack";
        public const string DraugrAttackVoice = "DraugrAttackVoice";
        public const string DraugrBark = "DraugrBark";
        public const string DraugrDamageVoice = "DraugrDamageVoice";
        public const string StoneGolemFootstep = "StoneGolemFootstep";
        public const string StoneGolemBark = "StoneGolemBark";
        public const string StoneGolemDownVoice = "StoneGolemDownVoice";
        public const string StoneGolemDeathVoice = "StoneGolemDeathVoice";

        public const string StoneGolemGroundStomp = "StoneGolemGroundStomp";
    }

    public static class Boss
    {
        public const string HandSweep = "BossHandSweep";
        public const string FootStomp = "BossFootStomp";
        public const string ChargePunch = "BossChargePunch";
        public const string RushAttack = "BossRushAttack";
        public const string RockEruption = "BossRockEruption";
        public const string MeteorImpact = "BossMeteorImpact";
        public const string Footstep = "BossFootstep";
        public const string OneLegBreakDownImpact = "BossOneLegBreakDownImpact";
        public const string TwoLegBreakDownImpact = "BossTwoLegBreakDownImpact";

        public const string HandSweepVoice = "BossHandSweepVoice";
        public const string FootStompVoice = "BossHandSweepVoice";
        public const string ChargePunchVoice = "BossChargePunchVoice";
        public const string RushAttackVoice = "BossRushAttackVoice";
        public const string RockEruptionVoice = "BossRockEruptionVoice";
        public const string MeteorVoice = "BossMeteorImpactVoice";
        public const string OneLegBreakVoice = "BossOneLegBreakVoice";
        public const string TwoLegBreakDownVoice = "BossTwoLegBreakDownVoice";
    }

    // Player_Voiceキューシート専用。SEとは異なるシートを指定して再生する。
    public static class PlayerVoice
    {
        public const string DamageLarge01 = "DamageVoiceLarge01";
        public const string DamageLarge02 = "DamageVoiceLarge02";
        public const string DamageMedium01 = "DamageVoiceMedium01";
        public const string DamageMedium02 = "DamageVoiceMedium02";
        public const string DamageSmall01 = "DamageVoiceSmall01";
        // 配布されているACBのキュー名は「Smal」。表記を補正すると音源を解決できない。
        public const string DamageSmall02 = "DamageVoiceSmal02";
        public const string Death = "DeathVoice";
        public const string IntroMovie = "IntroMovie_Voice";
        public const string ModeChange01 = "ModeChange_Voice_01";
        public const string ModeChange02 = "ModeChange_Voice_02";
        public const string ModeChange03 = "ModeChange_Voice_03";
        public const string Result = "Result_Voice";
        public const string Revive01 = "ReviveVoice01";
        public const string Revive02 = "ReviveVoice02";
        public const string SkillGet01 = "SkillGetVoice01";
        public const string SkillGet02 = "SkillGetVoice02";
        public const string SkillGet03 = "SkillGetVoice03";
        public const string SkillGet04 = "SkillGetVoice04";
        public const string SkillGet05 = "SkillGetVoice05";
        public const string SkillGet06 = "SkillGetVoice06";
        public const string ThunderCombo0101 = "Thunder_Voice_Combo_01_01";
        public const string ThunderCombo0102 = "Thunder_Voice_Combo_01_02";
        public const string ThunderCombo0103 = "Thunder_Voice_Combo_01_03";
        public const string ThunderCombo0104 = "Thunder_Voice_Combo_01_04";
        public const string ThunderCombo0201 = "Thunder_Voice_Combo_02_01";
        public const string ThunderCombo0202 = "Thunder_Voice_Combo_02_02";
        public const string ThunderCombo0203 = "Thunder_Voice_Combo_02_03";
        public const string ThunderCombo0204 = "Thunder_Voice_Combo_02_04";
        public const string ThunderCombo0301 = "Thunder_Voice_Combo_03_01";
        public const string ThunderCombo0302 = "Thunder_Voice_Combo_03_02";
        public const string ThunderCombo0303 = "Thunder_Voice_Combo_03_03";
        public const string ThunderCombo0304 = "Thunder_Voice_Combo_03_04";
        public const string ThunderCombo0305 = "Thunder_Voice_Combo_03_05";
        public const string ThunderCombo0306 = "Thunder_Voice_Combo_03_06";
        public const string ThunderCombo0401 = "Thunder_Voice_Combo_04_01";
        public const string ThunderCombo0402 = "Thunder_Voice_Combo_04_02";
        public const string ThunderCombo0403 = "Thunder_Voice_Combo_04_03";
        public const string ThunderCombo0404 = "Thunder_Voice_Combo_04_04";
        public const string ThunderCombo0405 = "Thunder_Voice_Combo_04_05";
        public const string ThunderCombo0406 = "Thunder_Voice_Combo_04_06";
        public const string ThunderCombo0501 = "Thunder_Voice_Combo_05_01";
        public const string ThunderCombo0502 = "Thunder_Voice_Combo_05_02";
        public const string ThunderCombo0503 = "Thunder_Voice_Combo_05_03";
        public const string ThunderCombo0504 = "Thunder_Voice_Combo_05_04";
        public const string ThunderCombo0505 = "Thunder_Voice_Combo_05_05";
        public const string ThunderCombo0506 = "Thunder_Voice_Combo_05_06";
        public const string ThunderCombo0507 = "Thunder_Voice_Combo_05_07";
        public const string WarriorAttack01 = "WarriorVoiceAttack01";
        public const string WarriorAttack02 = "WarriorVoiceAttack02";
        public const string WarriorAttack03 = "WarriorVoiceAttack03";
        public const string WarriorAttack04 = "WarriorVoiceAttack04";
        public const string WarriorAttack05 = "WarriorVoiceAttack05";
        public const string WarriorAttack06 = "WarriorVoiceAttack06";
    }

    public static class Environment
    {
        public const string VillageFire = "VillageFire";
    }

    public static class BGM
    {
        public const string Title = "OutGameIntro";
    }
}
