using System.Collections.Generic;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.XR;
using Bark.Gestures;
using Bark.Tools;
using BepInEx.Configuration;
using System;
using Bark.GUI;

namespace Bark.Modules.Movement
{
    internal class Frozone : BarkModule
    {
        public static readonly string DisplayName = "Frozone";

        private static GameObject IcePrefab;

        private static readonly Dictionary<bool, List<GameObject>> frozonicPlatforms = new();
        private static readonly Dictionary<bool, List<float>> frozonicTimes = new();
        private static readonly Dictionary<bool, int> platformIndex = new();

        public static ConfigEntry<string> Input;

        private const float lifetime = 2.5f;
        private const int maxPlatforms = 72;

        private bool leftActive;
        private bool rightActive;

        private InputTracker inputL;
        private InputTracker inputR;

        public override string GetDisplayName() => "Frozone";

        public override string Tutorial() =>
            $"Hold [{Input.Value.ToUpper()}] to create Ice Platforms you can Slide on.";

        protected override void Start()
        {
            base.Start();
            IcePrefab = Plugin.AssetBundle.LoadAsset<GameObject>("Ice");
        }

        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();

            ReloadConfiguration();
        }

        protected override void Cleanup()
        {
            Unsub();
        }

        protected override void ReloadConfiguration()
        {
            Unsub();

            // LEFT
            inputL = GestureTracker.Instance.GetInputTracker(Input.Value, XRNode.LeftHand);
            inputL.OnPressed += OnActivate;
            inputL.OnReleased += OnDeactivate;

            // RIGHT
            inputR = GestureTracker.Instance.GetInputTracker(Input.Value, XRNode.RightHand);
            inputR.OnPressed += OnActivate;
            inputR.OnReleased += OnDeactivate;
        }

        private void Unsub()
        {
            if (inputL != null)
            {
                inputL.OnPressed -= OnActivate;
                inputL.OnReleased -= OnDeactivate;
            }

            if (inputR != null)
            {
                inputR.OnPressed -= OnActivate;
                inputR.OnReleased -= OnDeactivate;
            }
        }

        private void OnActivate(InputTracker tracker)
        {
            if (tracker.node == XRNode.LeftHand) leftActive = true;
            if (tracker.node == XRNode.RightHand) rightActive = true;
        }

        private void OnDeactivate(InputTracker tracker)
        {
            if (tracker.node == XRNode.LeftHand) leftActive = false;
            if (tracker.node == XRNode.RightHand) rightActive = false;
        }

        public static void BindConfigEntries()
        {
            try
            {
                Input = Plugin.ConfigFile.Bind(
                    DisplayName,
                    "input",
                    "grip",
                    new ConfigDescription(
                        "Which button you press to activate the Ice Platform",
                        new AcceptableValueList<string>("grip", "trigger", "a/x", "b/y")
                    )
                );
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }

        private (Vector3 position, Quaternion rotation, Vector3 right) GetHand(bool left)
        {
            var hand = left
                ? GorillaTagger.Instance.leftHandTransform
                : GorillaTagger.Instance.rightHandTransform;

            return (hand.position, hand.rotation, hand.right);
        }

        private void FixedUpdate()
        {
            HandleFrozone(true);
            HandleFrozone(false);
        }

        public void HandleFrozone(bool left)
        {
            bool active = left ? leftActive : rightActive;

            if (!frozonicPlatforms.TryGetValue(left, out var list))
            {
                list = new List<GameObject>();
                frozonicPlatforms[left] = list;
            }

            if (!frozonicTimes.TryGetValue(left, out var times))
            {
                times = new List<float>();
                frozonicTimes[left] = times;
            }

            platformIndex.TryGetValue(left, out int index);

            if (active)
            {
                var hand = GetHand(left);

                GameObject platform;

                if (list.Count >= maxPlatforms)
                {
                    platform = list[index];
                }
                else
                {
                    platform = UnityEngine.Object.Instantiate(IcePrefab);

                    platform.transform.localScale *= GTPlayer.Instance.scale;

                    platform.AddComponent<GorillaSurfaceOverride>().overrideIndex = 61;

                    list.Add(platform);
                    times.Add(Time.time);
                }

                float offsetFloat = left ? 0.1f * GTPlayer.Instance.scale : 0f;
                Vector3 offset = hand.right * offsetFloat;

                platform.transform.position = hand.position + offset;
                platform.transform.rotation = hand.rotation;

                if (index < times.Count)
                    times[index] = Time.time;

                platformIndex[left] = (index + 1) % maxPlatforms;
            }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (Time.time - times[i] > lifetime)
                {
                    UnityEngine.Object.Destroy(list[i]);
                    list.RemoveAt(i);
                    times.RemoveAt(i);
                }
            }
        }
    }
}