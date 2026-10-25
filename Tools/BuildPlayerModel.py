import bpy
import math
import os
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
MODEL_DIR = os.path.join(ROOT, "Assets", "Models")
CAPTURE_DIR = os.path.join(ROOT, "Captures")
TOOLS_DIR = os.path.join(ROOT, "Tools")
os.makedirs(MODEL_DIR, exist_ok=True)
os.makedirs(CAPTURE_DIR, exist_ok=True)

# ----------------------------------------------------------------------------
# Clean scene
# ----------------------------------------------------------------------------
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.armatures, bpy.data.materials,
                   bpy.data.actions, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

scene = bpy.context.scene
scene.render.fps = 30
scene.frame_start = 1
scene.frame_end = 40
scene.unit_settings.system = "METRIC"
scene.unit_settings.scale_length = 1.0

# ----------------------------------------------------------------------------
# Materials
# ----------------------------------------------------------------------------
def make_material(name, color, roughness=0.68):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*color, 1.0)
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Metallic"].default_value = 0.0
    mat.diffuse_color = (*color, 1.0)
    return mat

MAT_JERSEY = make_material("Jersey", (0.10, 0.38, 0.95))
MAT_SKIN = make_material("Skin", (0.78, 0.56, 0.40))
MAT_SHORTS = make_material("Shorts", (0.055, 0.070, 0.110))
MAT_SOCKS = make_material("Socks", (0.88, 0.88, 0.84))
MAT_BOOTS = make_material("Boots", (0.025, 0.025, 0.030))
MAT_HAIR = make_material("Hair", (0.035, 0.025, 0.018))

# ----------------------------------------------------------------------------
# Geometry helpers
# ----------------------------------------------------------------------------
parts = []

def apply_transform(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    obj.select_set(False)

def finish_part(obj, name, material, bone, bevel=0.0):
    obj.name = name
    if material:
        obj.data.materials.append(material)
    if bevel > 0.0:
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        mod = obj.modifiers.new("Bevel", "BEVEL")
        mod.width = bevel
        mod.segments = 1
        bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.select_set(False)
    vg = obj.vertex_groups.new(name=bone)
    vg.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
    parts.append(obj)
    return obj

def add_box(name, loc, size, material, bone, rot=(0, 0, 0), bevel=0.018):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.scale = size
    apply_transform(obj)
    return finish_part(obj, name, material, bone, bevel)

def add_sphere(name, loc, size, material, bone, segments=12, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings,
                                         radius=1.0, location=loc)
    obj = bpy.context.object
    obj.scale = size
    apply_transform(obj)
    return finish_part(obj, name, material, bone, 0.0)

def add_cylinder_between(name, a, b, radius, material, bone, vertices=8):
    a = Vector(a)
    b = Vector(b)
    vec = b - a
    length = vec.length
    mid = (a + b) * 0.5
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius,
                                        depth=length, location=mid)
    obj = bpy.context.object
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = vec.normalized().to_track_quat("Z", "Y")
    apply_transform(obj)
    return finish_part(obj, name, material, bone, 0.0)

# ----------------------------------------------------------------------------
# Body geometry (Blender: Z up, character faces -Y)
# ----------------------------------------------------------------------------
# Pelvis / shorts
add_box("Pelvis", (0, 0, 0.98), (0.36, 0.24, 0.20), MAT_SHORTS, "Hips", bevel=0.035)
add_box("ShortsFront", (0, -0.055, 1.02), (0.34, 0.12, 0.13), MAT_SHORTS, "Hips", bevel=0.02)

# Torso
add_box("Abdomen", (0, 0, 1.14), (0.38, 0.25, 0.24), MAT_JERSEY, "Spine", bevel=0.04)
add_box("Chest", (0, 0, 1.36), (0.48, 0.28, 0.30), MAT_JERSEY, "Chest", bevel=0.045)
add_box("ChestFront", (0, -0.135, 1.36), (0.34, 0.035, 0.22), MAT_JERSEY, "Chest", bevel=0.01)

# Neck / head
add_cylinder_between("Neck", (0, 0, 1.49), (0, 0, 1.58), 0.058, MAT_SKIN, "Neck", vertices=8)
add_sphere("Head", (0, -0.005, 1.68), (0.165, 0.155, 0.185), MAT_SKIN, "Head", 16, 10)
add_sphere("Hair", (0, 0.035, 1.735), (0.178, 0.168, 0.155), MAT_HAIR, "Head", 16, 10)
# Simple face details on -Y side
add_sphere("Eye.L", (-0.060, -0.160, 1.700), (0.022, 0.012, 0.018), MAT_HAIR, "Head", 8, 6)
add_sphere("Eye.R", (0.060, -0.160, 1.700), (0.022, 0.012, 0.018), MAT_HAIR, "Head", 8, 6)
add_box("Brow", (0, -0.158, 1.726), (0.12, 0.012, 0.018), MAT_HAIR, "Head", bevel=0.002)

# Collar
add_cylinder_between("Collar", (0, 0, 1.48), (0, 0, 1.515), 0.085, MAT_JERSEY, "Chest", vertices=10)

# Arms
for side, suffix in ((-1, "L"), (1, "R")):
    sx = side
    shoulder = (0.28 * sx, 0, 1.44)
    elbow = (0.35 * sx, 0, 1.18)
    wrist = (0.40 * sx, 0, 0.96)
    hand = (0.415 * sx, 0, 0.89)

    add_sphere("Shoulder." + suffix, shoulder, (0.105, 0.095, 0.105),
               MAT_JERSEY, "UpperArm." + suffix, 10, 6)
    add_cylinder_between("UpperArm." + suffix, shoulder, elbow, 0.062,
                         MAT_JERSEY, "UpperArm." + suffix, vertices=8)
    add_cylinder_between("Forearm." + suffix, elbow, wrist, 0.050,
                         MAT_SKIN, "Forearm." + suffix, vertices=8)
    add_sphere("Hand." + suffix, hand, (0.070, 0.060, 0.085),
               MAT_SKIN, "Hand." + suffix, 10, 6)

# Legs
for side, suffix in ((-1, "L"), (1, "R")):
    sx = side
    hip = (0.12 * sx, 0, 0.94)
    knee = (0.14 * sx, 0, 0.52)
    ankle = (0.14 * sx, 0, 0.10)

    # Shorts cover upper thigh, skin lower thigh, both move with the thigh bone.
    add_cylinder_between("ShortsThigh." + suffix, hip, (0.13 * sx, 0, 0.74),
                         0.098, MAT_SHORTS, "Thigh." + suffix, vertices=8)
    add_cylinder_between("Thigh." + suffix, (0.13 * sx, 0, 0.78), knee,
                         0.083, MAT_SKIN, "Thigh." + suffix, vertices=8)
    add_cylinder_between("Shin." + suffix, knee, ankle,
                         0.066, MAT_SOCKS, "Shin." + suffix, vertices=8)
    add_box("Boot." + suffix, (0.14 * sx, -0.065, 0.055),
            (0.145, 0.28, 0.105), MAT_BOOTS, "Foot." + suffix, bevel=0.022)
    add_box("Toe." + suffix, (0.14 * sx, -0.215, 0.045),
            (0.125, 0.135, 0.080), MAT_BOOTS, "Toe." + suffix, bevel=0.018)

# ----------------------------------------------------------------------------
# Join mesh
# ----------------------------------------------------------------------------
bpy.ops.object.select_all(action="DESELECT")
for p in parts:
    p.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
mesh = bpy.context.object
mesh.name = "PlayerMesh"
mesh.data.name = "PlayerMesh"
mesh.select_set(False)

# ----------------------------------------------------------------------------
# Armature
# ----------------------------------------------------------------------------
arm_data = bpy.data.armatures.new("PlayerArmature")
arm = bpy.data.objects.new("Armature", arm_data)
bpy.context.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm
arm.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")

def bone(name, head, tail, parent=None):
    b = arm_data.edit_bones.new(name)
    b.head = head
    b.tail = tail
    b.roll = 0.0
    if parent:
        b.parent = arm_data.edit_bones[parent]
        b.use_connect = False
    return b

bone("Root", (0, 0, 0.0), (0, 0, 0.20))
bone("Hips", (0, 0, 0.92), (0, 0, 1.04), "Root")
bone("Spine", (0, 0, 1.04), (0, 0, 1.24), "Hips")
bone("Chest", (0, 0, 1.24), (0, 0, 1.46), "Spine")
bone("Neck", (0, 0, 1.46), (0, 0, 1.58), "Chest")
bone("Head", (0, 0, 1.58), (0, 0, 1.84), "Neck")

for side, suffix in ((-1, "L"), (1, "R")):
    sx = side
    bone("Shoulder." + suffix, (0, 0, 1.44), (0.28 * sx, 0, 1.44), "Chest")
    bone("UpperArm." + suffix, (0.28 * sx, 0, 1.44), (0.35 * sx, 0, 1.18), "Shoulder." + suffix)
    bone("Forearm." + suffix, (0.35 * sx, 0, 1.18), (0.40 * sx, 0, 0.96), "UpperArm." + suffix)
    bone("Hand." + suffix, (0.40 * sx, 0, 0.96), (0.42 * sx, 0, 0.86), "Forearm." + suffix)
    bone("Thigh." + suffix, (0.12 * sx, 0, 0.94), (0.14 * sx, 0, 0.52), "Hips")
    bone("Shin." + suffix, (0.14 * sx, 0, 0.52), (0.14 * sx, 0, 0.10), "Thigh." + suffix)
    bone("Foot." + suffix, (0.14 * sx, 0, 0.10), (0.14 * sx, -0.20, 0.05), "Shin." + suffix)
    bone("Toe." + suffix, (0.14 * sx, -0.20, 0.05), (0.14 * sx, -0.31, 0.04), "Foot." + suffix)

bpy.ops.object.mode_set(mode="OBJECT")
arm.select_set(False)

# Bind mesh
mod = mesh.modifiers.new("Armature", "ARMATURE")
mod.object = arm
mod.use_vertex_groups = True
mesh.parent = arm
mesh.matrix_parent_inverse = arm.matrix_world.inverted()

# ----------------------------------------------------------------------------
# Animations
# ----------------------------------------------------------------------------
if arm.animation_data is None:
    arm.animation_data_create()

for pb in arm.pose.bones:
    pb.rotation_mode = "XYZ"

def reset_pose():
    for pb in arm.pose.bones:
        pb.rotation_euler = (0.0, 0.0, 0.0)
        pb.location = (0.0, 0.0, 0.0)

def apply_pose(pose):
    for name, rot in pose.items():
        if name.endswith("_loc"):
            bone_name = name[:-4]
            if bone_name in arm.pose.bones:
                arm.pose.bones[bone_name].location = Vector(rot)
        elif name in arm.pose.bones:
            arm.pose.bones[name].rotation_euler = tuple(math.radians(v) for v in rot)

def make_action(name, keys):
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    arm.animation_data.action = action
    for frame, pose in keys:
        scene.frame_set(frame)
        reset_pose()
        apply_pose(pose)
        for pb in arm.pose.bones:
            pb.keyframe_insert(data_path="rotation_euler", frame=frame, group=pb.name)
            if pb.name == "Hips":
                pb.keyframe_insert(data_path="location", frame=frame, group=pb.name)
    action.use_frame_range = True
    action.frame_start = keys[0][0]
    action.frame_end = keys[-1][0]
    return action

idle_base = {
    "Hips_loc": (0, 0.0, 0),
    "Spine": (1, 0, 0),
    "Chest": (1, 0, 0),
    "Head": (0, 2, 0),
    "UpperArm.L": (0, 0, -3),
    "UpperArm.R": (0, 0, 3),
    "Forearm.L": (-4, 0, 0),
    "Forearm.R": (-4, 0, 0),
}
idle_mid = dict(idle_base)
idle_mid.update({
    "Hips_loc": (0, 0.008, 0),
    "Chest": (2, 1, 0),
    "Head": (0, -2, 0),
    "UpperArm.L": (2, 0, -5),
    "UpperArm.R": (-2, 0, 5),
})
make_action("Idle", [(1, idle_base), (20, idle_mid), (40, idle_base)])

def run_pose(phase):
    # phase 0: left leg forward, 1: right leg forward
    if phase == 0:
        return {
            "Hips_loc": (0, 0.015, 0),
            "Hips": (0, 4, 0),
            "Spine": (8, 0, 0),
            "Chest": (8, -4, 0),
            "Head": (-4, 0, 0),
            "Thigh.L": (32, 0, 0), "Shin.L": (-12, 0, 0), "Foot.L": (-10, 0, 0),
            "Thigh.R": (-32, 0, 0), "Shin.R": (-48, 0, 0), "Foot.R": (14, 0, 0),
            "UpperArm.L": (-30, 0, -4), "Forearm.L": (-30, 0, 0),
            "UpperArm.R": (30, 0, 4), "Forearm.R": (-30, 0, 0),
        }
    return {
        "Hips_loc": (0, 0.015, 0),
        "Hips": (0, -4, 0),
        "Spine": (8, 0, 0),
        "Chest": (8, 4, 0),
        "Head": (-4, 0, 0),
        "Thigh.L": (-32, 0, 0), "Shin.L": (-48, 0, 0), "Foot.L": (14, 0, 0),
        "Thigh.R": (32, 0, 0), "Shin.R": (-12, 0, 0), "Foot.R": (-10, 0, 0),
        "UpperArm.L": (30, 0, -4), "Forearm.L": (-30, 0, 0),
        "UpperArm.R": (-30, 0, 4), "Forearm.R": (-30, 0, 0),
    }

run_mid_a = {
    "Hips_loc": (0, 0.032, 0),
    "Spine": (9, 0, 0), "Chest": (9, 0, 0), "Head": (-5, 0, 0),
    "Thigh.L": (0, 0, 0), "Shin.L": (-30, 0, 0), "Foot.L": (-6, 0, 0),
    "Thigh.R": (0, 0, 0), "Shin.R": (-22, 0, 0), "Foot.R": (-6, 0, 0),
    "UpperArm.L": (0, 0, -4), "Forearm.L": (-25, 0, 0),
    "UpperArm.R": (0, 0, 4), "Forearm.R": (-25, 0, 0),
}
make_action("Run", [
    (1, run_pose(0)), (7, run_mid_a), (13, run_pose(1)),
    (19, run_mid_a), (25, run_pose(0)),
])

make_action("Turn", [
    (1, {"Hips_loc": (0, 0, 0), "Spine": (2, 0, 0)}),
    (8, {
        "Hips_loc": (0, 0.01, 0), "Hips": (0, 18, 0), "Spine": (4, 12, 0),
        "Chest": (4, 24, 0), "Head": (0, 20, 0),
        "Thigh.L": (-8, 0, 0), "Shin.L": (-18, 0, 0),
        "Thigh.R": (12, 0, 0), "Shin.R": (-12, 0, 0),
        "UpperArm.L": (-12, 0, -20), "UpperArm.R": (8, 0, 16),
    }),
    (16, {"Hips_loc": (0, 0, 0), "Spine": (2, 0, 0)}),
])

make_action("Brake", [
    (1, {"Hips_loc": (0, 0, 0), "Spine": (2, 0, 0)}),
    (6, {
        "Hips_loc": (0, -0.055, 0), "Spine": (-6, 0, 0), "Chest": (-8, 0, 0),
        "Head": (5, 0, 0),
        "Thigh.L": (18, 0, 0), "Shin.L": (-32, 0, 0), "Foot.L": (16, 0, 0),
        "Thigh.R": (18, 0, 0), "Shin.R": (-32, 0, 0), "Foot.R": (16, 0, 0),
        "UpperArm.L": (-24, 0, -16), "UpperArm.R": (-24, 0, 16),
        "Forearm.L": (-20, 0, 0), "Forearm.R": (-20, 0, 0),
    }),
    (12, {
        "Hips_loc": (0, -0.045, 0), "Spine": (-4, 0, 0), "Chest": (-6, 0, 0),
        "Thigh.L": (14, 0, 0), "Shin.L": (-28, 0, 0),
        "Thigh.R": (14, 0, 0), "Shin.R": (-28, 0, 0),
        "UpperArm.L": (-18, 0, -12), "UpperArm.R": (-18, 0, 12),
    }),
    (18, {"Hips_loc": (0, 0, 0), "Spine": (2, 0, 0)}),
])

make_action("Shoot", [
    (1, {"Hips_loc": (0, 0, 0), "Spine": (2, 0, 0)}),
    (6, {
        "Hips": (0, -10, 0), "Spine": (4, -8, 0), "Chest": (4, -16, 0),
        "Thigh.R": (-34, 0, 0), "Shin.R": (-38, 0, 0), "Foot.R": (14, 0, 0),
        "Thigh.L": (10, 0, 0), "Shin.L": (-16, 0, 0),
        "UpperArm.L": (16, 0, -10), "UpperArm.R": (-26, 0, 8),
    }),
    (12, {
        "Hips": (0, 12, 0), "Spine": (3, 10, 0), "Chest": (2, 24, 0),
        "Thigh.R": (48, 0, 0), "Shin.R": (-8, 0, 0), "Foot.R": (-18, 0, 0),
        "Thigh.L": (4, 0, 0), "Shin.L": (-10, 0, 0),
        "UpperArm.L": (-20, 0, -14), "UpperArm.R": (24, 0, 10),
    }),
    (18, {
        "Hips": (0, 6, 0), "Spine": (4, 6, 0), "Chest": (3, 12, 0),
        "Thigh.R": (24, 0, 0), "Shin.R": (-28, 0, 0), "Foot.R": (8, 0, 0),
        "Thigh.L": (2, 0, 0), "Shin.L": (-8, 0, 0),
        "UpperArm.L": (-8, 0, -8), "UpperArm.R": (10, 0, 6),
    }),
    (26, {"Hips_loc": (0, 0, 0), "Spine": (2, 0, 0)}),
])

make_action("Tackle", [
    (1, {"Hips_loc": (0, 0, 0), "Spine": (2, 0, 0)}),
    (6, {
        "Hips_loc": (0, -0.09, 0), "Spine": (12, 0, 0), "Chest": (14, 0, 0),
        "Head": (-10, 0, 0),
        "Thigh.L": (26, 0, 0), "Shin.L": (-28, 0, 0),
        "Thigh.R": (-12, 0, 0), "Shin.R": (-20, 0, 0),
        "UpperArm.L": (12, 0, -22), "UpperArm.R": (12, 0, 22),
    }),
    (12, {
        "Hips_loc": (0, -0.12, 0), "Spine": (18, 0, 0), "Chest": (20, 0, 0),
        "Head": (-14, 0, 0),
        "Thigh.L": (38, 0, 0), "Shin.L": (-34, 0, 0),
        "Thigh.R": (-20, 0, 0), "Shin.R": (-10, 0, 0),
        "UpperArm.L": (20, 0, -28), "UpperArm.R": (20, 0, 28),
    }),
    (20, {"Hips_loc": (0, 0, 0), "Spine": (2, 0, 0)}),
])

# Leave Idle active and reset for preview.
for action in bpy.data.actions:
    action.use_fake_user = True
arm.animation_data.action = bpy.data.actions.get("Idle")
scene.frame_set(1)
reset_pose()
apply_pose(idle_base)

# ----------------------------------------------------------------------------
# Lighting / camera / preview render
# ----------------------------------------------------------------------------
world = bpy.data.worlds.get("World") or bpy.data.worlds.new("World")
scene.world = world
world.use_nodes = True
bg = world.node_tree.nodes.get("Background")
if bg:
    bg.inputs[0].default_value = (0.055, 0.065, 0.080, 1.0)
    bg.inputs[1].default_value = 1.0

bpy.ops.object.light_add(type="AREA", location=(3.5, -4.0, 4.2))
key = bpy.context.object
key.data.energy = 700.0
key.data.shape = "DISK"
key.data.size = 4.0
key.rotation_euler = (math.radians(25), 0, math.radians(40))

bpy.ops.object.light_add(type="AREA", location=(-3.0, -2.0, 2.4))
fill = bpy.context.object
fill.data.energy = 250.0
fill.data.size = 3.0
fill.rotation_euler = (math.radians(60), 0, math.radians(-50))

bpy.ops.object.camera_add(location=(2.6, -4.6, 2.15))
cam = bpy.context.object
cam.data.lens = 55
target = Vector((0, 0, 1.0))
direction = target - cam.location
cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
scene.camera = cam

try:
    scene.render.engine = "BLENDER_EEVEE_NEXT"
except Exception:
    scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 700
scene.render.resolution_y = 700
scene.render.resolution_percentage = 100
scene.render.film_transparent = False
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = os.path.join(CAPTURE_DIR, "blender-player.png")
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(TOOLS_DIR, "Player.blend"))
try:
    bpy.ops.render.render(write_still=True)
    print("RENDERED_PREVIEW")
except Exception as exc:
    print("RENDER_FAILED", repr(exc))

# ----------------------------------------------------------------------------
# FBX export
# ----------------------------------------------------------------------------
bpy.ops.object.select_all(action="DESELECT")
arm.select_set(True)
mesh.select_set(True)
bpy.context.view_layer.objects.active = arm

fbx_path = os.path.join(MODEL_DIR, "Player.fbx")
bpy.ops.export_scene.fbx(
    filepath=fbx_path,
    use_selection=True,
    object_types={"ARMATURE", "MESH"},
    apply_scale_options="FBX_SCALE_ALL",
    global_scale=1.0,
    axis_forward="-Z",
    axis_up="Y",
    add_leaf_bones=False,
    primary_bone_axis="Y",
    secondary_bone_axis="X",
    use_mesh_modifiers=True,
    mesh_smooth_type="FACE",
    bake_anim=True,
    bake_anim_use_all_bones=True,
    bake_anim_use_nla_strips=False,
    bake_anim_use_all_actions=True,
    bake_anim_force_startend_keying=True,
    bake_anim_step=1,
    bake_anim_simplify_factor=0.0,
    path_mode="COPY",
    embed_textures=False,
)

print("EXPORTED_FBX", fbx_path)
print("ACTIONS", [a.name for a in bpy.data.actions])
print("MATERIALS", [m.name for m in bpy.data.materials])
