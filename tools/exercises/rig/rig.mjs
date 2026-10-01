// Exercise animations drawn from a rig: one figure (always the same body, proportions and colours, in the
// exercise thumbnails' palette) posed by joint angles at a few keyframes, eased between them and rendered frame by
// frame, so it moves smoothly without the figure ever changing. Equipment (barbell, dumbbells, bench, bar, cable...)
// is drawn with it. A spec is plain JSON (see poses/*.json); render one with
//   node rig.mjs poses/romanian_deadlift.json out.mp4 [--size 288] [--png frames-dir]
//
// Angles are in degrees, the direction a body part points from its joint: 0 straight down, 90 forward (the figure
// faces right), 180 straight up, -90 backwards. torso: hips → neck; head: neck → top of the head; upperArm: shoulder →
// elbow; forearm: elbow → wrist; thigh: hip → knee; shin: knee → ankle; foot: ankle → toes.
import { execFileSync } from "node:child_process";
import { mkdirSync, readFileSync, writeFileSync, rmSync, mkdtempSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { createRequire } from "node:module";

const args = process.argv.slice(2);
const opt = (name, fallback) => { const i = args.indexOf(`--${name}`); return i >= 0 ? args[i + 1] : fallback; };
const require = createRequire(opt("sharp-from", process.cwd() + "/"));
let sharpModule;
const sharp = (...a) => (sharpModule ??= require("sharp"))(...a);

// Body (units: the 720 px canvas the frames are drawn on).
const L = { torso: 172, neck: 20, head: 33, upperArm: 104, forearm: 96, hand: 16, thigh: 152, shin: 146, foot: 50 };
const C = {
  background: "#151821",
  body: "#C9CED8", bodyShade: "#A9B0BD", far: "#8E95A3", farShade: "#767D8B", outline: "#5B6170",
  muscle: "#3F7DFF", glow: "#3F7DFF",
  metal: "#7A818E", metalDark: "#4E5461", plate: "#3D424D", plateRim: "#8B92A0", wood: "#2B3040", pad: "#3A4050",
};

const rad = a => a * Math.PI / 180;
const dir = a => [Math.sin(rad(a)), Math.cos(rad(a))];
const add = (p, a, len) => [p[0] + dir(a)[0] * len, p[1] + dir(a)[1] * len];
const lerp = (a, b, t) => a + (b - a) * t;
const ease = t => 0.5 - 0.5 * Math.cos(Math.PI * t);

const defaults = { torso: 180, head: 180, upperArm: 0, forearm: 0, thigh: 0, shin: 0, foot: 90 };
const parts = ["upperArm", "forearm", "thigh", "shin", "foot"];

// A keyframe's angles with the far limbs (the arm and leg away from the viewer) filled in from the near ones.
function full(pose) {
  const p = { ...defaults, ...pose };
  for (const part of parts) {
    p[`${part}Far`] ??= p[part];
  }
  return p;
}

// Where every joint is for a pose, with the hips at the origin.
function joints(p) {
  const hip = [0, 0];
  const neck = add(hip, p.torso, L.torso);
  const shoulder = add(neck, p.torso + 180, 14);
  const headCenter = add(neck, p.head, L.neck + L.head);
  const limb = far => {
    const s = far ? "Far" : "";
    const elbow = add(shoulder, p[`upperArm${s}`], L.upperArm);
    const wrist = add(elbow, p[`forearm${s}`], L.forearm);
    const hand = add(wrist, p[`forearm${s}`], L.hand);
    const knee = add(hip, p[`thigh${s}`], L.thigh);
    const ankle = add(knee, p[`shin${s}`], L.shin);
    const toe = add(ankle, p[`foot${s}`], L.foot);
    const heel = add(ankle, p[`foot${s}`] + 180, 12);
    return { elbow, wrist, hand, knee, ankle, toe, heel };
  };
  return { hip, neck, shoulder, headCenter, near: limb(false), far: limb(true) };
}

// Pins the pose to the spec's anchor: the named joint (e.g. "near.ankle", "near.hand", "hip") at a fixed point,
// so feet stay on the floor, hands on the bar, hips on the bench.
function place(j, anchor, at) {
  const [path1, path2] = anchor.split(".");
  const point = path2 ? j[path1][path2] : j[path1];
  const dx = at[0] - point[0], dy = at[1] - point[1];
  const move = p => [p[0] + dx, p[1] + dy];
  const moveLimb = l => Object.fromEntries(Object.entries(l).map(([k, v]) => [k, move(v)]));
  return { hip: move(j.hip), neck: move(j.neck), shoulder: move(j.shoulder), headCenter: move(j.headCenter), near: moveLimb(j.near), far: moveLimb(j.far) };
}

// The pose at time t (0..1 over the loop): the keyframes played forward and back (or forward and round again with
// "loop": "forward"), easing in and out of each, with a pause at both ends of the rep.
function poseAt(spec, t) {
  const keys = spec.keyframes.map(k => ({ ...k, pose: full(k.pose) }));
  const forward = spec.loop === "forward";
  const path = forward ? [...keys, keys[0]] : [...keys, ...keys.slice(0, -1).reverse()];
  const holdUnits = spec.hold ?? 0.8;
  const segments = [];
  let time = 0;
  for (let i = 0; i < path.length - 1; i++) {
    if (i === 0 || (!forward && i === keys.length - 1)) {
      segments.push({ from: path[i], to: path[i], start: time, end: time + holdUnits });
      time += holdUnits;
    }
    const units = path[i + 1].units ?? 1;
    segments.push({ from: path[i], to: path[i + 1], start: time, end: time + units });
    time += units;
  }
  const at = t * time;
  const seg = segments.find(s => at >= s.start && at < s.end) ?? segments.at(-1);
  const local = ease(Math.min(1, Math.max(0, (at - seg.start) / (seg.end - seg.start || 1))));
  const pose = {};
  for (const key of Object.keys(seg.from.pose))
    pose[key] = lerp(seg.from.pose[key], seg.to.pose[key] ?? seg.from.pose[key], local);
  const anchorFrom = seg.from.at ?? spec.at, anchorTo = seg.to.at ?? spec.at;
  return {
    pose,
    at: [lerp(anchorFrom[0], anchorTo[0], local), lerp(anchorFrom[1], anchorTo[1], local)],
    anchor: seg.from.anchor ?? spec.anchor,
    props: interpolateProps(seg, local),
  };
}

// Keyframes may move props (a cable's pulley stays put, a band stretches); numbers in them ease too.
function interpolateProps(seg, t) {
  const a = seg.from.props ?? {}, b = seg.to.props ?? {};
  const out = { ...a };
  for (const [k, v] of Object.entries(b))
    if (typeof v === "number" && typeof a[k] === "number")
      out[k] = lerp(a[k], v, t);
  return out;
}

const seg = (a, b, width, color, extra = "") =>
  `<line x1="${a[0].toFixed(1)}" y1="${a[1].toFixed(1)}" x2="${b[0].toFixed(1)}" y2="${b[1].toFixed(1)}" stroke="${color}" stroke-width="${width}" stroke-linecap="round" ${extra}/>`;

// Shapes: each body part is a tube through a few points along it, each with its own radius (thick at the hip, a calf
// bulge, thin at the ankle), drawn as overlapping circles joined by quads; an outline is the same shape a bit wider.
const f1 = n => n.toFixed(1);
function tube(points) {
  let out = "";
  for (let i = 0; i < points.length; i++) {
    const [p, r] = points[i];
    out += `<circle cx="${f1(p[0])}" cy="${f1(p[1])}" r="${f1(r)}"/>`;
    if (i > 0) {
      const [q, rq] = points[i - 1];
      const d = [p[0] - q[0], p[1] - q[1]];
      const len = Math.hypot(...d) || 1;
      const n = [-d[1] / len, d[0] / len];
      out += `<polygon points="${f1(q[0] + n[0] * rq)},${f1(q[1] + n[1] * rq)} ${f1(p[0] + n[0] * r)},${f1(p[1] + n[1] * r)} ${f1(p[0] - n[0] * r)},${f1(p[1] - n[1] * r)} ${f1(q[0] - n[0] * rq)},${f1(q[1] - n[1] * rq)}"/>`;
    }
  }
  return out;
}
const widen = (points, by) => points.map(([p, r]) => [p, r + by]);

// A point `t` of the way from a to b, moved `front` units towards the part's front (the figure faces right).
function along(a, b, t, front, flip = 1) {
  const d = [b[0] - a[0], b[1] - a[1]];
  const len = Math.hypot(...d) || 1;
  const n = [d[1] / len * flip, -d[0] / len * flip];
  return [a[0] + d[0] * t + n[0] * front, a[1] + d[1] * t + n[1] * front];
}

// The body as three kinds of chain, each drawn as one shape (outlined once, so no seams at the joints): the torso
// with neck, a leg with its foot, an arm with its hand. Points are [position, radius].
function chains(j, l) {
  const torso = [[along(j.hip, j.neck, -0.04, -6, -1), 37], [along(j.hip, j.neck, 0.36, 2, -1), 34],
    [along(j.hip, j.neck, 0.72, 9, -1), 45], [along(j.hip, j.neck, 0.93, 3, -1), 36], [j.neck, 20],
    [add(j.neck, j.headAngle, L.neck + 6), 14]];
  return {
    torso: [torso],
    leg: [[[j.hip, 30], [along(j.hip, l.knee, 0.35, 3), 26], [along(j.hip, l.knee, 0.75, 1), 20], [l.knee, 16],
      [along(l.knee, l.ankle, 0.28, -5), 19], [along(l.knee, l.ankle, 0.7, -1), 12], [l.ankle, 10]],
      [[l.heel, 11], [l.ankle, 10], [along(l.ankle, l.toe, 0.6, 0), 9], [l.toe, 7]]],
    arm: [[[j.shoulder, 21], [along(j.shoulder, l.elbow, 0.45, 2), 17], [l.elbow, 12], [along(l.elbow, l.wrist, 0.3, 1), 14],
      [l.wrist, 9], [l.hand, 10]]],
  };
}

// Where on the body each muscle is: [chain, from joint, to joint, side (1 front, -1 back, 0 all of it), from, to].
const MUSCLES = {
  Chest: ["torso", "hip", "neck", 1, 0.58, 0.9], Abs: ["torso", "hip", "neck", 1, 0.1, 0.52],
  Back: ["torso", "hip", "neck", -1, 0.45, 0.92], LowerBack: ["torso", "hip", "neck", -1, 0.08, 0.45],
  Traps: ["torso", "hip", "neck", -1, 0.88, 1.08], Glutes: ["torso", "hip", "neck", -1, -0.12, 0.1],
  Neck: ["torso", "neck", "head", 0, 0.05, 0.6], Shoulders: ["arm", "shoulder", "elbow", 0, -0.06, 0.28],
  Biceps: ["arm", "shoulder", "elbow", 1, 0.25, 0.85], Triceps: ["arm", "shoulder", "elbow", -1, 0.2, 0.88],
  Forearms: ["arm", "elbow", "wrist", 0, 0.1, 0.75], Quads: ["leg", "hip", "knee", 1, 0.12, 0.85],
  Hamstrings: ["leg", "hip", "knee", -1, 0.12, 0.88], Calves: ["leg", "knee", "ankle", -1, 0.12, 0.6],
};

const polyline = (a, b, width, color, extra = "") =>
  `<line x1="${f1(a[0])}" y1="${f1(a[1])}" x2="${f1(b[0])}" y2="${f1(b[1])}" stroke="${color}" stroke-width="${f1(width)}" stroke-linecap="round" ${extra}/>`;

let clipId = 0;
// One chain: outline, fill, a soft shade down its back and light down its front, and the working muscles on it
// glowing blue, masked to the chain so nothing leaves its outline.
function drawChain(kind, tubes, j, l, muscles, light, shade, strength) {
  const id = `c${clipId++}`;
  const shape = tubes.map(tube).join("");
  let out = `<g fill="${C.outline}">${tubes.map(t => tube(widen(t, 2.5))).join("")}</g><g fill="${light}">${shape}</g>`;
  // A mask, not a clipPath: librsvg merges a clipPath's shapes into one path, where overlapping shapes wound
  // opposite ways cancel out and leave stripes.
  out += `<mask id="${id}" maskUnits="userSpaceOnUse" x="-5000" y="-5000" width="10000" height="10000"><g fill="#fff">${shape}</g></mask><g mask="url(#${id})">`;
  const point = name => name === "hip" ? j.hip : name === "neck" ? j.neck : name === "shoulder" ? j.shoulder
    : name === "head" ? j.headCenter : l[name];
  for (const m of muscles) {
    const spec = MUSCLES[m];
    if (!spec || spec[0] !== kind)
      continue;
    const [, fromJoint, toJoint, side, from, to] = spec;
    const a = point(fromJoint), b = point(toJoint);
    const flip = kind === "torso" ? -1 : 1;
    const r = kind === "torso" ? 40 : kind === "leg" ? 23 : 15;
    // The muscle belly: an oval along the bone on its side of the limb, glowing.
    const c = along(a, b, (from + to) / 2, side * r * 0.32, flip);
    const length = Math.hypot(b[0] - a[0], b[1] - a[1]) * (to - from) / 2;
    const width = side === 0 ? r * 1.05 : r * 0.72;
    const angle = Math.atan2(b[1] - a[1], b[0] - a[0]) * 180 / Math.PI;
    // Drawn as a path (librsvg smears a blurred, rotated <ellipse> inside a clip into stripes).
    const ellipse = (rx, ry, fill, extra) => {
      const cos = Math.cos(angle * Math.PI / 180), sin = Math.sin(angle * Math.PI / 180);
      const pts = Array.from({ length: 36 }, (_, i) => {
        const t = i / 36 * 2 * Math.PI, x = Math.cos(t) * rx, y = Math.sin(t) * ry;
        return `${f1(c[0] + x * cos - y * sin)},${f1(c[1] + x * sin + y * cos)}`;
      });
      return `<polygon points="${pts.join(" ")}" fill="${fill}" ${extra}/>`;
    };
    out += ellipse(length * 1.05, width * 1.2, C.glow, `filter="url(#glow)" opacity="${f1(0.9 * strength)}"`);
    out += ellipse(length, width, "url(#muscle)", `opacity="${f1(0.95 * strength)}"`);
  }
  return out + "</g>";
}

// Equipment, drawn behind (bench, rack, bar) or in front of the body (what the hands hold).
function propsBehind(j, props) {
  const out = [];
  if (props.bench) {
    const { x, y, length = 300, height } = props.bench;
    const legH = height ?? (props.floor - y);
    out.push(`<rect x="${x - length / 2}" y="${y}" width="${length}" height="22" rx="8" fill="${C.pad}" stroke="${C.outline}" stroke-width="3"/>`);
    out.push(seg([x - length / 2 + 30, y + 22], [x - length / 2 + 30, y + legH], 14, C.metalDark));
    out.push(seg([x + length / 2 - 30, y + 22], [x + length / 2 - 30, y + legH], 14, C.metalDark));
  }
  if (props.pullupBar) {
    const { x, y, floor } = props.pullupBar;
    out.push(seg([x - 150, y], [x - 150, floor], 12, C.metalDark), seg([x + 150, y], [x + 150, floor], 12, C.metalDark));
    out.push(seg([x - 160, y], [x + 160, y], 10, C.metal));
  }
  if (props.cable) {
    const { x, y } = props.cable;
    out.push(`<rect x="${x - 16}" y="${Math.min(y, props.floor ?? 640) - 10}" width="32" height="${Math.abs((props.floor ?? 640) - y) + 10}" rx="6" fill="${C.wood}" opacity="0.7"/>`);
  }
  return out.join("");
}

function propsFront(j, props) {
  const out = [];
  const hand = j.near.hand, handFar = j.far.hand;
  if (props.barbell) {
    // Side on, a barbell is its plate: a big disc centred on the hands.
    const c = [(hand[0] + handFar[0]) / 2, (hand[1] + handFar[1]) / 2];
    const r = props.barbell.plate ?? 62;
    out.push(`<circle cx="${c[0].toFixed(1)}" cy="${c[1].toFixed(1)}" r="${r}" fill="${C.plate}" stroke="${C.plateRim}" stroke-width="5"/>`);
    out.push(`<circle cx="${c[0].toFixed(1)}" cy="${c[1].toFixed(1)}" r="${r * 0.55}" fill="none" stroke="${C.metalDark}" stroke-width="4"/>`);
    out.push(`<circle cx="${c[0].toFixed(1)}" cy="${c[1].toFixed(1)}" r="9" fill="${C.metal}"/>`);
  }
  if (props.dumbbells) {
    for (const h of [hand]) {
      out.push(`<circle cx="${h[0].toFixed(1)}" cy="${h[1].toFixed(1)}" r="24" fill="${C.plate}" stroke="${C.plateRim}" stroke-width="4"/>`);
      out.push(`<circle cx="${h[0].toFixed(1)}" cy="${h[1].toFixed(1)}" r="7" fill="${C.metal}"/>`);
    }
  }
  if (props.kettlebell) {
    const h = hand;
    out.push(`<circle cx="${h[0].toFixed(1)}" cy="${(h[1] + 34).toFixed(1)}" r="28" fill="${C.plate}" stroke="${C.plateRim}" stroke-width="4"/>`);
    out.push(`<path d="M${h[0] - 14},${h[1] + 12} Q${h[0]},${h[1] - 12} ${h[0] + 14},${h[1] + 12}" stroke="${C.plateRim}" stroke-width="6" fill="none"/>`);
  }
  if (props.cable) {
    out.push(seg(hand, [props.cable.x, props.cable.y], 3, C.plateRim));
    out.push(`<circle cx="${props.cable.x}" cy="${props.cable.y}" r="12" fill="${C.metal}"/>`);
  }
  return out.join("");
}

// The camera for the whole loop: a square around everything the figure reaches over the rep (plus the equipment),
// with a margin, so nothing leaves the frame and the camera never moves.
function viewBox(spec) {
  let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
  const take = p => { minX = Math.min(minX, p[0]); maxX = Math.max(maxX, p[0]); minY = Math.min(minY, p[1]); maxY = Math.max(maxY, p[1]); };
  for (let i = 0; i <= 48; i++) {
    const { pose, at, anchor } = poseAt(spec, i / 48);
    const j = place(joints(pose), anchor, at);
    for (const p of [j.hip, j.neck, j.shoulder, ...Object.values(j.near), ...Object.values(j.far)])
      take(p);
    take([j.headCenter[0] - L.head, j.headCenter[1] - L.head]);
    take([j.headCenter[0] + L.head, j.headCenter[1] + L.head]);
  }
  const props = spec.props ?? {};
  if (props.pullupBar) { take([props.pullupBar.x - 160, props.pullupBar.y - 10]); take([props.pullupBar.x + 160, props.pullupBar.y]); }
  if (props.bench) { take([props.bench.x - (props.bench.length ?? 300) / 2, props.bench.y]); take([props.bench.x + (props.bench.length ?? 300) / 2, props.bench.y + 30]); }
  if (props.cable) take([props.cable.x, props.cable.y]);
  const size = Math.max(maxX - minX, maxY - minY) * 1.08 + 40;
  const cx = (minX + maxX) / 2, cy = (minY + maxY) / 2;
  return [cx - size / 2, cy - size / 2, size];
}

function frameSvg(spec, t, box = viewBox(spec)) {
  const { pose, at, anchor, props: keyProps } = poseAt(spec, t);
  const props = { ...spec.props, ...keyProps };
  const j = place(joints(pose), anchor, at);
  j.torsoAngle = pose.torso;
  j.headAngle = pose.head;
  const muscles = spec.muscles ?? [];
  const far = chains(j, j.far), near = chains(j, j.near);
  // Far arm and leg first, darker, then the torso and head, the near leg, the near arm.
  const body = drawChain("arm", far.arm, j, j.far, muscles, C.far, C.farShade, 0.5)
    + drawChain("leg", far.leg, j, j.far, muscles, C.far, C.farShade, 0.5)
    + `<circle cx="${f1(j.headCenter[0])}" cy="${f1(j.headCenter[1])}" r="${L.head + 2.5}" fill="${C.outline}"/>`
    + `<circle cx="${f1(j.headCenter[0])}" cy="${f1(j.headCenter[1])}" r="${L.head}" fill="${C.body}"/>`
    + drawChain("torso", near.torso, j, j.near, muscles, C.body, C.bodyShade, 1)
    + drawChain("leg", near.leg, j, j.near, muscles, C.body, C.bodyShade, 1);
  const nearArm = drawChain("arm", near.arm, j, j.near, muscles, C.body, C.bodyShade, 1);
  const [bx, by, bs] = box;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="720" height="720" viewBox="${bx.toFixed(1)} ${by.toFixed(1)} ${bs.toFixed(1)} ${bs.toFixed(1)}">
<defs><filter id="glow" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="7"/></filter><filter id="soft" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="3"/></filter><radialGradient id="muscle" gradientUnits="objectBoundingBox"><stop offset="0" stop-color="#7FA8FF"/><stop offset="0.6" stop-color="#3F7DFF"/><stop offset="1" stop-color="#2E62D9"/></radialGradient></defs>
<rect x="${bx - 10}" y="${by - 10}" width="${bs + 20}" height="${bs + 20}" fill="${C.background}"/>
${propsBehind(j, props)}
${body}
${nearArm}
${propsFront(j, props)}
</svg>`;
}

export async function render(spec, file, { size = 288, fps = 24, seconds = spec.seconds ?? 3.2, pngDir = null } = {}) {
  const work = mkdtempSync(join(tmpdir(), "rig-"));
  try {
    const count = Math.round(fps * seconds);
    const box = viewBox(spec);
    for (let i = 0; i < count; i++) {
      const png = await sharp(Buffer.from(frameSvg(spec, i / count, box))).resize(size, size).png().toBuffer();
      writeFileSync(join(work, `f${String(i).padStart(4, "0")}.png`), png);
      if (pngDir) {
        mkdirSync(pngDir, { recursive: true });
        writeFileSync(join(pngDir, `f${String(i).padStart(4, "0")}.png`), png);
      }
    }
    execFileSync("ffmpeg", ["-loglevel", "error", "-y", "-framerate", String(fps), "-i", join(work, "f%04d.png"), "-c:v", "libx264",
      "-profile:v", "main", "-pix_fmt", "yuv420p", "-preset", "slow", "-crf", "22", "-movflags", "+faststart", "-an", file]);
  } finally {
    rmSync(work, { recursive: true, force: true });
  }
}

export { frameSvg, viewBox };

if (process.argv[1]?.endsWith("rig.mjs")) {
  const [specFile, outFile] = args.filter(a => !a.startsWith("--") && !args[args.indexOf(a) - 1]?.startsWith("--"));
  await render(JSON.parse(readFileSync(specFile, "utf8")), outFile, { size: +opt("size", "288"), pngDir: opt("png", null) });
  console.log(`wrote ${outFile}`);
}
