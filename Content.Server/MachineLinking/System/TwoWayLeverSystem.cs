using Content.Server.MachineLinking.Components;
using Content.Shared.Interaction;
using Content.Shared.MachineLinking;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server.MachineLinking.System
{
    public sealed class TwoWayLeverSystem : EntitySystem
    {
        [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        const string _leftToggleImage = "rotate_ccw.svg.192dpi.png";
        const string _rightToggleImage = "rotate_cw.svg.192dpi.png";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TwoWayLeverComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<TwoWayLeverComponent, ActivateInWorldEvent>(OnActivated);
            SubscribeLocalEvent<TwoWayLeverComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
        }

        private void OnInit(EntityUid uid, TwoWayLeverComponent component, ComponentInit args)
        {
            _signalSystem.EnsureTransmitterPorts(uid, component.LeftPort, component.RightPort, component.MiddlePort);
        }

        private void OnActivated(EntityUid uid, TwoWayLeverComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            component.State = component.State switch
            {
                TwoWayLeverState.Middle => component.NextSignalLeft ? TwoWayLeverState.Left : TwoWayLeverState.Right,
                TwoWayLeverState.Right => TwoWayLeverState.Middle,
                TwoWayLeverState.Left => TwoWayLeverState.Middle,
                _ => throw new ArgumentOutOfRangeException()
            };

            StateChanged(uid, component);

            args.Handled = true;
        }

        private void OnGetInteractionVerbs(EntityUid uid, TwoWayLeverComponent component, GetVerbsEvent<InteractionVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract || (args.Hands == null))
                return;

            InteractionVerb verbLeft = new()
            {
                Act = () =>
                {
                    component.State = component.State switch
                    {
                        TwoWayLeverState.Middle => TwoWayLeverState.Left,
                        TwoWayLeverState.Right => TwoWayLeverState.Middle,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    StateChanged(uid, component);
                },
                Category = VerbCategory.Lever,
                Message = Loc.GetString("two-way-lever-cant"),
                Disabled = component.State == TwoWayLeverState.Left,
                Icon = new SpriteSpecifier.Texture(new ($"/Textures/Interface/VerbIcons/{_leftToggleImage}")),
                Text = Loc.GetString("two-way-lever-left"),
            };

            args.Verbs.Add(verbLeft);

            InteractionVerb verbRight = new()
            {
                Act = () =>
                {
                    component.State = component.State switch
                    {
                        TwoWayLeverState.Left => TwoWayLeverState.Middle,
                        TwoWayLeverState.Middle => TwoWayLeverState.Right,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    StateChanged(uid, component);
                },
                Category = VerbCategory.Lever,
                Message = Loc.GetString("two-way-lever-cant"),
                Disabled = component.State == TwoWayLeverState.Right,
                Icon = new SpriteSpecifier.Texture(new ($"/Textures/Interface/VerbIcons/{_rightToggleImage}")),
                Text = Loc.GetString("two-way-lever-right"),
            };

            args.Verbs.Add(verbRight);
        }

        private void StateChanged(EntityUid uid, TwoWayLeverComponent component)
        {
            if (component.State == TwoWayLeverState.Middle)
                component.NextSignalLeft = !component.NextSignalLeft;

            if (TryComp(uid, out AppearanceComponent? appearance))
                _appearance.SetData(uid, TwoWayLeverVisuals.State, component.State, appearance);

            var port = component.State switch
            {
                TwoWayLeverState.Left => component.LeftPort,
                TwoWayLeverState.Right => component.RightPort,
                TwoWayLeverState.Middle => component.MiddlePort,
                _ => throw new ArgumentOutOfRangeException()
            };

            _signalSystem.InvokePort(uid, port);
        }
    }
}
