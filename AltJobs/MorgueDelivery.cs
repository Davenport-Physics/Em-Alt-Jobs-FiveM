using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;

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

        private bool job_started     = false;
        private Random rand          = new Random();

        private int last_mission_idx = -1;
        private int mission_idx      = 0;
        
        private List<Mission> missions = new List<Mission>();

        public MorgueDelivery()
        {

            EventHandlers["arp_alt_jobs:client:GiveMorgueBlob"] += new Action<string>(SetMissions);
            TriggerServerEvent("arp_alt_jobs:server:GetMorgueBlob");
            CheckDistanceToMorgue();

        }

        private void SetMissions(string missions_blob)
        {

            MissionBlobs blobs = JsonConvert.DeserializeObject<MissionBlobs>(missions_blob);
            for (int i = 0; i < blobs.MorgueMissions.Count; i++)
            {
                this.missions.Add(new Mission(blobs.MorgueMissions[i]));
            }

        }

        private async void CheckDistanceToMorgue()
        {
            while (this.missions.Count == 0)
            {
                await Delay(5000);
            }

            const int time_delay_far_away = 1000 * 10;
            while (true)
            {
                this.distance_to_marker = Vector3.Distance(Game.PlayerPed.Position, this.morgue_point);

                if (this.distance_to_marker >= 200)
                    await Delay(time_delay_far_away * 2);
                else if (this.distance_to_marker >= 75)
                    await Delay(time_delay_far_away);
                else
                    await CheckForNewJobOrExisting();

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
                World.DrawMarker(MarkerType.VerticalCylinder, this.marker_pos, this.marker_dir, this.marker_rot, this.marker_scale, this.marker_color);
        }

        private void DrawTextIfNecessary()
        {
            if (this.distance_to_marker <= 3 && !this.job_started)
                Shared.DrawTextSimple("Press ~g~Enter~w~ to start job.");
        }

        private void HandleInput()
        {
            if (this.distance_to_marker <= 3 && !this.job_started && API.IsControlJustPressed(1, 18))
            {
                if (API.IsPedInAnyVehicle(Game.PlayerPed.Handle, false) && API.GetPedInVehicleSeat(API.GetVehiclePedIsIn(Game.PlayerPed.Handle, false), -1) == Game.PlayerPed.Handle)
                    StartJob();
                else
                    Exports["mythic_notify"].SendAlert("inform", "You must be in a vehicle.", 5000);
            }
        }

        private void StartJob()
        {
            this.job_started = true;
            this.mission_idx = rand.Next(this.missions.Count);

            while (this.mission_idx == this.last_mission_idx)
                this.mission_idx = rand.Next(this.missions.Count);

            this.last_mission_idx = this.mission_idx;
            this.missions[this.mission_idx].RunJob();
        }

    }
}
