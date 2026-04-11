using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using GorillaLocomotion;
using Bark.Extensions;
using Bark.Gestures;
using Bark.GUI;
using Bark.Tools;
using UnityEngine;
using UnityEngine.XR;

namespace Bark.Modules.Multiplayer;

public class Telekinesis : BarkModule
{
    public static readonly string DisplayName = "Telekinesis";
    public static Telekinesis Instance;
    public SphereCollider tkCollider;
    private readonly List<TKMarker> markers = new();

    private Joint joint;
    private ParticleSystem playerParticles, sithlordHandParticles;
    private AudioSource sfx;
    private TKMarker sithLord;

    private void Awake()
    {
        Instance = this;
    }

    private void FixedUpdate()
    {
        if (ControllerInputPoller.instance.rightGrab) { OnGrip(); } else { if (isCopying == true || whoCopy != null || skipRay == true || theRig != null) { isCopying = false; whoCopy = null; skipRay = false; theRig = null; } }
        if (ControllerInputPoller.TriggerFloat(XRNode.RightHand) > 0.5f) { trigR = true; } else { trigR = false; }


        if (Time.frameCount % 300 == 0)
            DistributeMidichlorians();

        if (!sithLord) TryGetSithLord();

        if (sithLord)
        {
            var rb = GTPlayer.Instance.bodyCollider.attachedRigidbody;
            if (!sithLord.IsGripping() || (SelectSith && sithLord.rig != ChosenSith))
            {
                sithLord = null;
                sfx.Stop();
                sithlordHandParticles.Stop();
                sithlordHandParticles.Clear();
                playerParticles.Stop();
                playerParticles.Clear();
                rb.velocity = GTPlayer.Instance.bodyVelocityTracker.GetAverageVelocity(true) * 2;
                return;
            }

            var end = sithLord.controllingHand.position + sithLord.controllingHand.up * 3 * sithLord.rig.scaleFactor;
            var direction = end - GTPlayer.Instance.bodyCollider.transform.position;
            rb.AddForce(direction * 10, ForceMode.Impulse);
            var dampingThreshold = direction.magnitude * 10;
            //if (rb.velocity.magnitude > dampingThreshold)
            //if(direction.magnitude < 1)
            rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, .1f);
        }
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        try
        {
            ReloadConfiguration();
            var prefab = Plugin.AssetBundle.LoadAsset<GameObject>("TK Hitbox");
            var hitbox = Instantiate(prefab);
            hitbox.name = "Bark TK Hitbox";
            hitbox.transform.SetParent(GTPlayer.Instance.bodyCollider.transform, false);
            hitbox.layer = BarkInteractor.InteractionLayer;
            tkCollider = hitbox.GetComponent<SphereCollider>();
            tkCollider.isTrigger = true;
            playerParticles = hitbox.GetComponent<ParticleSystem>();
            playerParticles.Stop();
            playerParticles.Clear();
            sfx = hitbox.GetComponent<AudioSource>();

            var sithlordEffect = Instantiate(prefab);
            sithlordEffect.name = "Bark Sithlord Particles";
            sithlordEffect.transform.SetParent(GTPlayer.Instance.bodyCollider.transform, false);
            sithlordEffect.layer = BarkInteractor.InteractionLayer;
            sithlordHandParticles = sithlordEffect.GetComponent<ParticleSystem>();
            var shape = sithlordHandParticles.shape;
            shape.radius = .2f;
            shape.position = Vector3.zero;
            Destroy(sithlordEffect.GetComponent<SphereCollider>());
            DistributeMidichlorians();
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    bool trigR = false;
    VRRig ChosenSith = null;
    bool isCopying = false;
    VRRig whoCopy = null;
    bool skipRay = false;
    VRRig theRig = null;
    void OnGrip()
    {
        if (SelectSith)
        {
            UnityEngine.Physics.Raycast(GorillaTagger.Instance.rightHandTransform.position, GorillaTagger.Instance.rightHandTransform.forward, out var Ray, 512f);

            Vector3 StartPosition = GorillaTagger.Instance.rightHandTransform.position;
            Vector3 EndPosition = skipRay ? theRig.transform.position : isCopying ? whoCopy.transform.position : Ray.point;

            GameObject NewPointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            NewPointer.GetComponent<Renderer>().material.shader = Shader.Find("GUI/Text Shader");
            NewPointer.GetComponent<Renderer>().material.color = skipRay ? new Color(0f, 0f, 0f, 1f) : isCopying ? new Color(0f, 0f, 0f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
            NewPointer.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            NewPointer.transform.position = EndPosition;

            UnityEngine.Object.Destroy(NewPointer.GetComponent<BoxCollider>());
            UnityEngine.Object.Destroy(NewPointer.GetComponent<Rigidbody>());
            UnityEngine.Object.Destroy(NewPointer.GetComponent<Collider>());
            UnityEngine.Object.Destroy(NewPointer, Time.deltaTime);

            GameObject line = new GameObject("Line");
            LineRenderer liner = line.AddComponent<LineRenderer>();
            liner.material.shader = Shader.Find("GUI/Text Shader");
            liner.startColor = skipRay ? new Color(0f, 0f, 0f, 1f) : isCopying ? new Color(0f, 0f, 0f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
            liner.endColor = skipRay ? new Color(0f, 0f, 0f, 1f) : isCopying ? new Color(0f, 0f, 0f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
            liner.startWidth = 0.025f;
            liner.endWidth = 0.025f;
            liner.positionCount = 2;
            liner.useWorldSpace = true;
            liner.SetPosition(0, StartPosition);
            liner.SetPosition(1, EndPosition);
            UnityEngine.Object.Destroy(line, Time.deltaTime);

            if (trigR && Ray.collider.GetComponentInParent<VRRig>() != null)
            {
                isCopying = true;
                whoCopy = Ray.collider.GetComponentInParent<VRRig>();

                if (ChosenSith != whoCopy && whoCopy != null)
                {
                    ChosenSith = whoCopy;
                }

                skipRay = true;
                theRig = whoCopy;
            }
            else
            {
                isCopying = false;
                whoCopy = null;
            }
        }
    }

    public static ConfigEntry<bool> SelectPlayer;
    public static void BindConfigEntries()
    {
        try
        {
            SelectPlayer = Plugin.ConfigFile.Bind(
                section: DisplayName,
                key: "allow gun",
                defaultValue: false,
                description: "Whether or not only one selected Person can throw you around"
            );
        }
        catch (Exception e) { Logging.Exception(e); }
    }

    bool SelectSith = false;
    protected override void ReloadConfiguration()
    {
        SelectSith = SelectPlayer.Value;
    }

    private void TryGetSithLord()
    {
        foreach (var tk in markers)
            try
            {
                if (tk && tk.IsGripping() && tk.PointingAtMe() && (SelectSith ? tk.rig == ChosenSith : true))
                {
                    sithLord = tk;
                    playerParticles.Play();
                    sithlordHandParticles.transform.SetParent(tk.controllingHand);
                    sithlordHandParticles.transform.localPosition = Vector3.zero;
                    sithlordHandParticles.Play();
                    sfx.Play();
                    break;
                }
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
    }

    private void DistributeMidichlorians()
    {
        foreach (var rig in VRRigCache.ActiveRigs)
            try
            {
                if (rig.OwningNetPlayer.IsLocal ||
                    rig.gameObject.GetComponent<TKMarker>()) continue;

                markers.Add(rig.gameObject.AddComponent<TKMarker>());
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
    }

    protected override void Cleanup()
    {
        foreach (var m in markers) m?.Obliterate();
        tkCollider?.gameObject?.Obliterate();
        sithlordHandParticles?.gameObject?.Obliterate();
        joint?.Obliterate();
        sithLord = null;
        markers.Clear();
        tkCollider = null;
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "If another Player Points their index Finger at you, they can pick you up with Telekinesis.\n" +
               "Use the allow Gun Setting to only allow one selected Player to use Telekinesis.";
    }

    public class TKMarker : MonoBehaviour
    {
        public static int count;
        public VRRig rig;
        public Transform leftHand, rightHand, controllingHand;
        public Rigidbody controllingBody;
        private DebugRay dr;
        private bool grippingRight, grippingLeft;
        private int uuid;

        private void Awake()
        {
            rig = GetComponent<VRRig>();
            uuid = count++;
            leftHand = SetupHand("L");
            rightHand = SetupHand("R");
            dr = new GameObject($"{uuid} (Debug Ray)").AddComponent<DebugRay>();
        }

        private void OnDestroy()
        {
            dr?.gameObject?.Obliterate();
            leftHand?.GetComponent<Rigidbody>()?.Obliterate();
            rightHand?.GetComponent<Rigidbody>()?.Obliterate();
        }

        public Transform SetupHand(string hand)
        {
            var handTransform = transform.Find(
                string.Format(GestureTracker.palmPath, hand).Substring(1)
            );
            var rb = handTransform.gameObject.AddComponent<Rigidbody>();

            rb.isKinematic = true;
            return handTransform;
        }

        public bool IsGripping()
        {
            grippingRight =
                rig.rightIndex.calcT < .5f &&
                rig.rightMiddle.calcT > .5f;
            //rig.rightThumb.calcT > .5f;

            grippingLeft =
                rig.leftIndex.calcT < .5f &&
                rig.leftMiddle.calcT > .5f;
            //rig.leftThumb.calcT > .5f;
            return grippingRight || grippingLeft;
        }

        public bool PointingAtMe()
        {
            try
            {
                if (!(grippingRight || grippingLeft)) return false;
                var hand = grippingRight ? rightHand : leftHand;
                controllingHand = hand;
                if (!hand) return false;
                controllingBody = hand?.GetComponent<Rigidbody>();
                if (!controllingBody) return false;
                RaycastHit hit;
                var ray = new Ray(hand.position, hand.up);
                Logging.Debug("DOING THE THING WITH THE COLLIDER");
                var collider = Instance.tkCollider;
                UnityEngine.Physics.SphereCast(ray, .2f * GTPlayer.Instance.scale, out hit, collider.gameObject.layer);
                return hit.collider == collider;
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }

            return false;
        }
    }
}