# Battle Presentation Clash Engagement

## V1 Contract

`BattleClashEngagementProfile` is the single tuning source for the shared
ClashReady approach used by `BattlePresentationSandbox` and `BattleScene`.
Both environments use the same character World Prefabs, world scale, profile,
and `BattleClashEngagementResolver`.

V1 implements `BothAdvance` only:

- Final gap comes from an unordered pair override when one exists.
- Otherwise final gap is default gap plus both character spacing offsets.
- Character spacing is keyed by the stable presentation key stored on the
  character World Prefab. It describes presentation spacing, not sprite width.
- Relative speed changes each side's movement share, not the final gap,
  animation duration, clip speed, Slash, Hit, HitStop, or Fade timing.
- Both sides start and finish the approach together using the same duration.
- When the current gap is already at or below the final gap, actors are not
  pushed apart.

The current 1v1 runtime-validated working baseline for the default gap is
`2.2`. This is not final art tuning. Future distance tuning must continue
through the shared default gap, character clash spacing offsets, or unordered
pair gap overrides rather than introducing Sandbox or scene-local gap fields.

## Relative Speed

For non-negative speeds A and B:

```text
relativeAdvantage = (A - B) / (A + B)
A share = 0.5 + relativeAdvantage * influence
B share = 1 - A share
```

When `A + B` is effectively zero, both shares are `0.5`. Shares are clamped by
the profile's minimum and maximum movement shares. The approach distance is
split by these shares, so a faster actor moves farther while both actors still
arrive at ClashReady simultaneously.

## Multi-Clash Scheduling (Future)

The current runtime demo is 1v1 and does not implement multi-clash scheduling.
Relative speed currently affects only the WorldRoot movement share inside one
clash pair.

Future 2v2, 3v3, or 4v4 execution must not wait for Clash #1 to finish fully
before Clash #2 can even begin its approach. The target is **Just-In-Time
ClashReady**: while an earlier clash is playing, a later independent pair may
begin its non-rule approach early enough to reach ClashReady near its actual
execution turn. A short ClashReady wait is acceptable, but actors should not
arrive several clashes early and remain standing for a long time.

Combat order remains authoritative. `BattleExecutionRunner` and combat rules
still determine the real Action, Roll, Damage, Impact, ActionComplete, and turn
order. Presentation scheduling may prepare a future independent pair's visual
approach, but animation overlap must never reorder or commit combat events.

Relative speed remains pair-relative. Given independent pairs A/X, B/Y, and
C/Z, each pair calls `BattleClashEngagementResolver` separately. A future
global scheduler decides when each approach starts; it does not combine every
actor's speed into one global movement formula.

### Actor Reservation

Future scheduling requires actor reservation or actor locking. Independent
pairs such as A/X and B/Y may approach concurrently or with staggered starts.
If Clash #1 is A/X and Clash #3 is A/Z, Clash #3 cannot control A's WorldRoot
until Clash #1 releases A. One actor may have only one presentation sequence
holding WorldRoot motion authority at a time.

### Waiting Tolerance

Just-In-Time does not require the Roll to occur on the exact frame a pair
reaches ClashReady. A small presentation waiting tolerance is expected. Its
value must be tuned against the future multiplayer combat rhythm and is not
fixed by this V1 document.

Possible future responsibility concepts include:

- `MultiClashPresentationSchedule`
- `ClashPresentationSlot`
- `ExpectedExecutionOrder`
- `ApproachStart`
- `ClashReadyETA`
- `ActorReservation`
- `WaitingTolerance`

These names describe design responsibilities only; they are not implemented
classes or current API commitments.

Runtime scheduling should begin only when one execution cycle can contain two
or more independent clash pairs. The current 1v1 demo does not need it. Current
priority remains character-specific animation integration, formal Tie and
Guard presentation completion, and presentation work required by the current
demo. Multi-clash scheduling does not block those tasks.

## HeavyImpact (Future)

HeavyImpact is a result or presentation tag. Its default detection contract is:

```text
single committed damage >= target max HP * 0.20
```

This uses one damage value that has actually been committed and the target's
maximum HP. It does not use current HP, predicted damage, or presentation-side
damage calculation.

HeavyImpact is not a fixed knockback distance. Knockback distance and duration,
shake, HitStop, and hit animation remain independently configurable. A future
HeavyImpact tag may select stronger feedback or a different re-engagement mode.

## Re-engagement Modes (Future)

V1 uses `BothAdvance` for normal engagement and re-engagement. A future
HeavyImpact default may select `AttackerOnly`: the recovering defender stays in
place while the attacker alone advances until the shared ClashReady gap is
restored. `AttackerOnly` is documentation only in V1.

Future selection priority is:

```text
Skill or card explicit override
> HeavyImpact default behavior
> Normal BothAdvance
```

A future explicit override may force HeavyImpact or select `BothAdvance`,
`AttackerOnly`, `DefenderOnly`, or `Custom`. V1 does not add this skill system.

## Sandbox Workflow

Character animation work must preserve parity between Sandbox and BattleScene:

```text
same WorldRoot scale
+ same character World Prefab
+ same Engagement Profile
+ same Engagement Resolver
```

When Sandbox tests character A against character B, its ClashReady starting
space must match BattleScene under the same pair and speed inputs. Character
specific animation can then be tuned in Sandbox and consumed by BattleScene
without copying distance parameters into a second scene-owned source.
