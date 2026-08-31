public static class SceneBlackboardKeys
{
    public static class Player
    {
        public static class Flashlight
        {
            public const string IsEquipped = "player_flashlight_isEquipped";
            public const string CanEquip = "player_flashlight_canEquip";
        }

        public static class Mop
        {
            public const string IsEquipped = "player_mop_isEquipped";
            public const string Interactable = "player_mop_interactable";
        }

        public static class Shotgun
        {
            public const string IsEquipped = "player_shotgun_isEquipped";
            public const string CanEquip = "player_shotgun_canEquip";
        }

        public const string CanSprint = "player_canSprint";
        public const string CanJump = "player_canJump";
        public const string CanCrouch = "player_canCrouch";
        public const string CanInteract = "player_canInteract";
        public const string LastKnownPosition = "player_lastKnownPosition";
        public const string NoiseScore = "player_noiseScore";
        public const string InOffice = "player_inOffice";
    }

    public static class Scene
    {
        public const string Day = "scene_day";
        public const string DayDescription = "scene_dayDescription";
        public const string DeerCount = "scene_deer_count";
        public const string WolfCount = "scene_wolf_count";

        public static class Objectives
        {
            public const string MainObjective = "scene_objectives_mainObjective";
            public const string SubObjective = "scene_objectives_subObjective";
            public const string ObjectiveCount = "scene_objectives_objectiveCount";
        }

        public static class Decals
        {
            public static class Example
            {
                public const string Interactable = "scene_decals_id_interactable";
                public const string Removed = "scene_decals_id_removed";
            }

            public static class Sprint
            {
                public const string Interactable = "scene_decals_sprint_interactable";
                public const string Removed = "scene_decals_sprint_removed";
            }
        }

        public static class Barriers
        {
            public static class Example
            {
                public const string IsActive = "scene_barriers_id_isActive";
            }

            public static class SquareEntrance
            {
                public const string IsActive = "scene_barriers_squareentrance_isActive";
            }
        }

        public static class Waypoints
        {
            public static class Example
            {
                public const string IsActive = "scene_waypoints_id_isActive";
            }

            public static class Office
            {
                public const string IsActive = "scene_waypoints_office_isActive";
            }

            public static class Graffiti
            {
                public const string IsActive = "scene_waypoints_graffiti_isActive";
            }

            public static class Generator
            {
                public const string IsActive = "scene_waypoints_generator_isActive";
            }
        }
    }

    public static class Deer
    {
        public const string Interactable = "deer_interactable";
        public const string CanFlee = "deer_canFlee";
        public const string CanWander = "deer_canWander";
        public const string Lureable = "deer_lureable";
        public const string Lured = "deer_lured";
        public const string Fed = "deer_fed";
    }

    public static class Wolf
    {
        public const string Interactable = "wolf_interactable";
        public const string CanFlee = "wolf_canFlee";
        public const string CanWander = "wolf_canWander";
        public const string Lureable = "wolf_lureable";
        public const string Lured = "wolf_lured";
        public const string Fed = "wolf_fed";
    }

    public static class Door
    {
        public const string Interactable = "door_interactable";
        public const string Locked = "door_locked";
        public const string Opened = "door_opened";
        public const string HasKey = "door_hasKey";
    }

    public static class LightSwitch
    {
        public const string Interactable = "lightSwitch_interactable";
        public const string Enabled = "lightSwitch_enabled";
    }

    public static class Generator
    {
        public const string Interactable = "generator_interactable";
        public const string IsRunning = "generator_isRunning";
    }

    public static class Computer
    {
        public const string Interactable = "computer_interactable";
        public const string Interacted = "computer_interacted";
    }

    public static class Phone
    {
        public const string Interactable = "phone_interactable";
        public const string Interacted = "phone_interacted";
        public const string Ringing = "phone_ringing";
        public const string Speaking = "phone_speaking";
    }

    public static class Suffix
    {
        public const string Completed = "_completed";
    }
}
