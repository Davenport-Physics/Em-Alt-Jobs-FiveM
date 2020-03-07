using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace AltJobs
{
    public class Mission : BaseScript
    {
        public readonly int base_pay;
        public readonly int base_timer;

        public readonly string mission_name;
        public readonly Vector3 end_point;
        public readonly ItemIdentifiers items;

        private bool is_job_done = false;

        private int mission_timer;
        private int mission_timer_yellow;
        private int mission_timer_red;
        private int mission_item;
        private string mission_sound_file;
        private string mission_sound_file_end;
        private string mission_item_name;

        private float distance_to_end;

        private static Vector3 marker_dir = new Vector3(0, 0, 0);
        private static Vector3 marker_rot = new Vector3(0, 0, 0);
        private static Vector3 marker_scale = new Vector3(4f, 4f, 4f);
        private static Color marker_color = Color.FromArgb(150, 255, 255, 0);

        public Mission()
        {

        }

        public Mission(MissionBlob blob)
        {
            this.mission_name = blob.Location;
            this.base_pay     = blob.BasePay;
            this.base_timer   = blob.AvailableTimeInSecs * 1000;
            this.end_point    = new Vector3(blob.Coords[0], blob.Coords[1], blob.Coords[2]);
            this.items        = new ItemIdentifiers(blob.Items);
        }

        public Mission(string mission_name, int base_pay, int base_timer, Vector3 end_point, ItemIdentifiers items)
        {
            this.mission_name = mission_name;
            this.base_pay     = base_pay;
            this.base_timer   = base_timer;
            this.end_point    = end_point;
            this.items        = items;
        }

        public async void RunJob()
        {
            await InitJob();
            while (!is_job_done)
            {
                await Delay(5);
                DrawTimeLeft();
                DrawEndPointIfNearby();
                HandleIfPlayerIsOnMarker();
                CheckIfRanOutOfTime();
            }
        }

        public async Task InitJob()
        {

            this.is_job_done = false;
            this.mission_item = new Random().Next(this.items.item_names.Count);
            this.mission_sound_file = this.items.sound_files[this.mission_item];
            this.mission_sound_file_end = this.items.end_sound_files[this.mission_item];
            this.mission_item_name = this.items.item_names[this.mission_item];

            TriggerEvent("addItem", this.mission_item_name, 1, true);

            PlayMissionDialog();
            await FadeOut();
            SetTimer();
            API.SetNewWaypoint(this.end_point[0], this.end_point[1]);
        }

        private void PlayMissionDialog()
        {
            Exports["PlayExternalSounds"].PlaySound(this.mission_sound_file, .3f);
        }

        private async Task FadeOut()
        {
            API.DoScreenFadeOut(1000);
            await Delay(7500);
            API.DoScreenFadeIn(2500);
        }

        public bool IsJobDone()
        {
            return this.is_job_done;
        }

        private void DrawTimeLeft()
        {
            int diff = (int)((this.mission_timer - API.GetGameTimer()) * 0.001);
            if (diff >= this.mission_timer_yellow)
                Shared.DrawTextSimple(string.Format("~g~{0}~w~ seconds left", diff));
            else if (diff >= this.mission_timer_red)
                Shared.DrawTextSimple(string.Format("~y~{0}~w~ seconds left", diff));
            else
                Shared.DrawTextSimple(string.Format("~r~{0}~w~ seconds left", diff));
        }

        private void CheckIfRanOutOfTime()
        {
            if (API.GetGameTimer() >= this.mission_timer)
            {
                this.is_job_done = true;
                Exports["mythic_notify"].SendAlert("inform", string.Format("You have failed to deliver {0} in time.", this.mission_item_name), 5000);
                API.ClearGpsPlayerWaypoint();
            }
        }

        private void DrawEndPointIfNearby()
        {
            this.distance_to_end = Vector3.Distance(Game.PlayerPed.Position, this.end_point);
            if (this.distance_to_end <= 30)
                World.DrawMarker(MarkerType.VerticalCylinder, this.end_point, Mission.marker_dir, Mission.marker_rot, Mission.marker_scale, Mission.marker_color);
        }

        private void SetTimer()
        {
            this.mission_timer        = API.GetGameTimer() + this.base_timer;
            this.mission_timer_yellow = (int)(this.base_timer * 0.001 * (2.0f / 3.0f));
            this.mission_timer_red    = (int)(this.base_timer * 0.001 * (1.0f / 3.0f));
        }

        private async void HandleIfPlayerIsOnMarker()
        {
            if (this.distance_to_end <= 3.0)
            {
                int payout = CalcPayout();
                this.is_job_done = true;

                Exports["PlayExternalSounds"].PlaySound(this.mission_sound_file_end, .3f);
                Exports["mythic_notify"].SendAlert("inform", string.Format("You have successfully delivered {0}", this.mission_item_name), 5000);
                TriggerEvent("removeItem", this.mission_item_name, 1);
                await FadeOut();
                TriggerServerEvent("arp_alt_jobs:server:add_money", payout);
                Exports["mythic_notify"].SendAlert("inform", string.Format("You made ${0}", payout), 5000);
            }
        }

        private int CalcPayout()
        {
            return this.base_pay + (int)((this.mission_timer - API.GetGameTimer()) * .01);
        }

    }

    public class ItemIdentifiers
    {
        public readonly List<string> sound_files;
        public readonly List<string> end_sound_files;
        public readonly List<string> item_names;

        public ItemIdentifiers(List<string> sound_files, List<string> end_sound_files, List<string> item_names)
        {
            this.sound_files = sound_files;
            this.end_sound_files = end_sound_files;
            this.item_names = item_names;
        }

        public ItemIdentifiers(List<ItemObject> items)
        {
            List<string> item_names      = new List<string>();
            List<string> sound_files     = new List<string>();
            List<string> end_sound_files = new List<string>();
            for (int i = 0;i < items.Count; i++)
            {
                item_names.Add(items[i].Item);
                sound_files.Add(items[i].IntroSound);
                end_sound_files.Add(items[i].OutroSound);
            }
            this.item_names      = item_names;
            this.sound_files     = sound_files;
            this.end_sound_files = end_sound_files;
        }
    }

    public class ItemObject
    {
        public string Item;
        public string IntroSound;
        public string OutroSound;
    }

    public class MissionBlob
    {
        public string Location;
        public List<float> Coords;
        public int AvailableTimeInSecs;
        public int BasePay;
        public List<ItemObject> Items;
    }

    public class MissionBlobs
    {
        public List<MissionBlob> MorgueMissions;
    }
}
