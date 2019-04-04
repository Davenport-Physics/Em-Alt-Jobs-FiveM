using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace AltJobs
{
    class MorgueDelivery : BaseScript
    {
        private readonly Vector3 morgue_point = new Vector3(264.7073f, -1351.095f, 32.93513f);
        private float distance_to_marker;

        private Vector3 marker_pos   = new Vector3(264.7073f, -1351.095f, 30.93513f);
        private Vector3 marker_dir   = new Vector3(0, 0, 0);
        private Vector3 marker_rot   = new Vector3(0, 0, 0);
        private Vector3 marker_scale = new Vector3(4f, 4f, 4f);
        private Color  marker_color  = Color.FromArgb(150, 255, 255, 0);

        private bool job_started = false;

        private Random rand = new Random();

        private int mission_idx  = 0;
        private List<Mission> missions = new List<Mission>()
        {
            new Mission("blackmarket", 2500, new Vector3(3788.968f, 4462.44f, 5.27f), new ItemIdentifiers(new List<string>(){ "", "", "" }, new List<string>(){ "Human heart", "Human brain", "Human bones" })),
            new Mission("docks", 1500, new Vector3(166.0794f, -3299.002f, 5.28f), new ItemIdentifiers(new List<string>(){  "", "", "" }, new List<string>(){ "Human juice", "Questionable Chemicals", "Embalming Fluid" })),
            new Mission("airport", 2000 , new Vector3(-1279.879f, -2864.998f, 13.24499f), new ItemIdentifiers(new List<string>(){  "", "", "" }, new List<string>(){ "Human Bones", "Human Teeth", "Human Liver" }))
        };

        public MorgueDelivery()
        {
            CheckDistanceToPoint();
        }

        private async void CheckDistanceToPoint()
        {
            while (true)
            {
                this.distance_to_marker = Vector3.Distance(Game.PlayerPed.Position, this.morgue_point);
                if (this.distance_to_marker >= 75)
                {
                    await Delay(5000);
                }
                else
                {
                    await CheckForNewJobOrExisting();
                }
            }
        }

        private async Task CheckForNewJobOrExisting()
        {
            if (this.job_started)
            {
                this.job_started = !this.missions[this.mission_idx].IsJobDone();
                await Delay(1000);
            }
            else
            {
                await Delay(5);
                HandleDrawingAndInput();
            }
        }

        private void HandleDrawingAndInput()
        {
            DrawSceneMarkerIfNeeded();
            DrawTextIfNecessary();
            HandleInput();
        }

        private void DrawSceneMarkerIfNeeded()
        {
            if (this.distance_to_marker <= 25)
            {
                World.DrawMarker(MarkerType.VerticalCylinder, this.marker_pos, this.marker_dir, this.marker_rot, this.marker_scale, this.marker_color);
            }
        }

        private void DrawTextIfNecessary()
        {
            if (this.distance_to_marker <= 3 && !this.job_started)
            {
                Shared.DrawTextSimple("Press ~g~Enter~w~ to start job.");
            }
        }

        private void HandleInput()
        {
            if (this.distance_to_marker <= 3 && !this.job_started && API.IsControlJustPressed(1, 18))
            {
                if (API.IsPedInAnyVehicle(Game.PlayerPed.Handle, false))
                {
                    StartJob();
                } else
                {
                    TriggerEvent("ShowInformationLeft", 5000, "You must be in a vehicle.");
                }
            }
        }

        private void StartJob()
        {
            this.job_started = true;
            this.mission_idx = rand.Next(this.missions.Count);
            this.missions[this.mission_idx].RunJob();
        }

    }

    public class Mission : BaseScript
    {
        public readonly int base_pay;
        public readonly string mission_name;
        public readonly Vector3 end_point;
        public readonly ItemIdentifiers items;

        private static Random rand = new Random();

        private bool is_job_done = false;

        private int mission_item;
        private string mission_sound_file;
        private string mission_item_name;

        private float distance_to_end;

        private static Vector3 marker_dir = new Vector3(0, 0, 0);
        private static Vector3 marker_rot = new Vector3(0, 0, 0);
        private static Vector3 marker_scale = new Vector3(4f, 4f, 4f);
        private static Color marker_color = Color.FromArgb(150, 255, 255, 0);

        public Mission()
        {

        }

        public Mission(string mission_name, int base_pay, Vector3 end_point, ItemIdentifiers items)
        {
            this.mission_name = mission_name;
            this.base_pay     = base_pay;
            this.end_point    = end_point;
            this.items        = items;
        }

        public async void RunJob()
        {
            InitJob();
            while (!is_job_done)
            {
                await Delay(5);
                DrawEndPointIfNearby();
                HandleIfPlayerIsOnMarker();
            }
        }

        public void InitJob()
        {
            this.mission_item       = rand.Next(this.items.item_names.Count);
            this.mission_sound_file = this.items.sound_files[this.mission_item];
            this.mission_item_name  = this.items.item_names[this.mission_item];

            PlayMissionDialog();
            FadeOut();
            API.SetNewWaypoint(this.end_point[0], this.end_point[1]);
        }

        private void PlayMissionDialog()
        {
            Exports["PlayExternalSounds"].PlaySound(this.mission_sound_file, .3f);
        }

        private async void FadeOut()
        {
            API.DoScreenFadeOut(2500);
            await Delay(10000);
            API.DoScreenFadeIn(2500);
        }

        public bool IsJobDone()
        {
            return this.is_job_done;
        }

        private void DrawEndPointIfNearby()
        {
            this.distance_to_end = Vector3.Distance(Game.PlayerPed.Position, this.end_point);
            if (this.distance_to_end <= 30)
            {
                World.DrawMarker(MarkerType.VerticalCylinder, this.end_point, Mission.marker_dir, Mission.marker_rot, Mission.marker_scale, Mission.marker_color);
            }
        }

        private void HandleIfPlayerIsOnMarker()
        {
            if (this.distance_to_end <= 1.0)
            {
                this.is_job_done = true;
                TriggerEvent("ShowInformationLeft", 2500, "You successfully delivered something");
            }
        }

    }

    public struct ItemIdentifiers
    {
        public readonly List<string> sound_files;
        public readonly List<string> item_names;

        public ItemIdentifiers(List<string> sound_files, List<string> item_names)
        {
            this.sound_files = sound_files;
            this.item_names  = item_names;
        }
    }
}
