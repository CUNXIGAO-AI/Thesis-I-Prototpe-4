using System.Collections.Generic;
using UnityEngine;

namespace MalbersAnimations.Controller
{
    public class RagdollDeath : State
    {
        [Header("Ragdoll")]
        [Tooltip("Ragdoll prefab that will replace the current animal controller")]
        public GameObject ragdollPrefab;

        public float Drag = 0.1f;
        public float AngularDrag = 0.1f;

        [Header("Material Change")]
        [Tooltip("Delay before changing materials after death")]
        public float materialChangeDelay = 3f;
        
        [Tooltip("New material to apply to all meshes after death")]
        public Material deathMaterial;

        private GameObject ragdollInstance;

        public bool EnablePreProcessing = true;
        public CollisionDetectionMode collision = CollisionDetectionMode.ContinuousSpeculative;

        public override string StateName => "Death/Ragdoll Replace";
        public override string StateIDName => "Death";

        public override void Activate()
        {
            animal.Mode_Stop();
            animal.Mode_Interrupt();
            base.Activate();
            Replace();
        }

        public void Replace()
        {
            // 所有现有的ragdoll替换代码保持不变
            ragdollInstance = Instantiate(ragdollPrefab, transform.position, transform.rotation);

            // 保持原有代码不变...
            var AllJoints = ragdollInstance.GetComponentsInChildren<CharacterJoint>();
            foreach (var joint in AllJoints) { joint.enablePreprocessing = true; }
            
            // 需要禁用它，否则当我们复制层次结构对象的位置/旋转时，ragdoll每次都会试图"纠正"连接的关节，导致畸形/故障实例
            ragdollInstance.SetActive(false);

            //匹配根骨骼
            ragdollInstance.transform.SetPositionAndRotation(transform.position, transform.rotation);

            //将所有动物骨骼映射到字典中
            var animalBones = animal.RootBone.GetComponentsInChildren<Transform>();
            var AnimalBoneMap = new Dictionary<string, Transform>();
            foreach (Transform bone in animalBones) AnimalBoneMap[bone.name] = bone;

            //将Ragdoll中的所有骨骼映射到字典中
            var ragdollBones = ragdollInstance.GetComponentsInChildren<Transform>();

            foreach (var bn in ragdollBones)
            {
                //匹配Animal骨骼到Ragdoll骨骼的位置和旋转
                if (AnimalBoneMap.TryGetValue(bn.name, out Transform root))
                {
                    bn.SetPositionAndRotation(root.position, root.rotation);
                }
            }

            animal.Anim.enabled = false; //禁用Animator

            //禁用/移除ragdoll中的所有mesh渲染器
            var allSkinnedMeshRendererRagdoll = ragdollInstance.GetComponentsInChildren<SkinnedMeshRenderer>();
            var allMeshRendererRagdoll = ragdollInstance.GetComponentsInChildren<MeshRenderer>();

            foreach (var rdoll in allSkinnedMeshRendererRagdoll)
            {
                Destroy(rdoll.gameObject);
            }

            foreach (var rdoll in allMeshRendererRagdoll)
            {
                Destroy(rdoll.gameObject);
            }

            var allSkinnedMeshRendererAnimal = animal.GetComponentsInChildren<SkinnedMeshRenderer>(false);
            var allMeshRendererAnimal = animal.GetComponentsInChildren<MeshRenderer>(false);

            var allLODs = animal.GetComponentsInChildren<LODGroup>();

            //将相机的LODs更改
            foreach (var lod in allLODs)
            {
                lod.transform.parent = ragdollInstance.transform;
            }

            //将所有蒙皮网格渲染器移动到Ragdoll
            foreach (var rdoll in allSkinnedMeshRendererAnimal)
            {
                if (rdoll.gameObject.activeInHierarchy)
                {
                    if (rdoll.GetComponentInParent<LODGroup>() == null)
                    {
                        rdoll.transform.parent = ragdollInstance.transform;
                    }
                }
                RemapSkinToNewBones(rdoll, ragdollInstance.transform);
            }

            //将所有网格渲染器移动到Ragdoll
            foreach (var rdoll in allMeshRendererAnimal)
            {
                if (rdoll.gameObject.activeInHierarchy)
                {
                    if (rdoll.GetComponentInParent<LODGroup>() == null)
                    {
                        var Parent = ragdollInstance.transform.FindGrandChild(rdoll.transform.parent.name) ??
                            ragdollInstance.transform.FindGrandChild(rdoll.transform.parent.parent.name);

                        rdoll.transform.parent = Parent;
                    }
                }
            }

            // 原有的力应用代码保持不变...
            Vector3 HitDirection = Vector3.zero;
            Vector3 HitPoint = Vector3.zero;
            Collider HitCollider = null;
            ForceMode ForceMod = ForceMode.VelocityChange;

            if (animal.TryGetComponent<IMDamage>(out var IMDamage))
            {
                HitDirection = IMDamage.HitDirection;
                HitPoint = IMDamage.HitPosition;
                HitCollider = IMDamage.HitCollider;
                ForceMod = IMDamage.LastForceMode;
            }

            MDebug.Draw_Arrow(HitPoint, HitDirection.normalized * 3, Color.yellow, 5);

            var ragdollRB = ragdollInstance.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in ragdollRB)
            {
                rb.collisionDetectionMode = collision;
                rb.velocity = animal.RB.velocity;  //将动物的速度匹配到ragdoll上
                rb.drag = Drag;
                rb.angularDrag = AngularDrag;

                if (HitCollider != null && HitCollider.name.Contains(rb.name)) //查找碰撞体和刚体
                {
                    rb.AddForce(HitDirection, ForceMod);
                }
            }

            animal.OnStateChange.Invoke(ID);//调用事件!!

            // 在这里添加一个简单的组件来处理延迟材质变更
            if (deathMaterial != null)
            {
                var materialChanger = ragdollInstance.AddComponent<DelayedMaterialChanger>();
                materialChanger.material = deathMaterial;
                materialChanger.delay = materialChangeDelay;
            }

            ragdollInstance.SetActive(true);

            animal.Delay_Action(() =>
            {
                Destroy(animal.gameObject);
            });
        }

        // 原有的RemapSkinToNewBones方法保持不变
        private void RemapSkinToNewBones(SkinnedMeshRenderer thisRenderer, Transform RootBone)
        {
            if (thisRenderer == null) return;

            var OldRootBone = thisRenderer.rootBone;

            var NewBones = RootBone.GetComponentsInChildren<Transform>();

            Dictionary<string, Transform> boneMap = new();

            foreach (Transform t in NewBones)
            {
                boneMap[t.name] = t;
            }

            Transform[] boneArray = thisRenderer.bones;

            for (int idx = 0; idx < boneArray.Length; ++idx)
            {
                string boneName = boneArray[idx].name;

                if (false == boneMap.TryGetValue(boneName, out boneArray[idx]))
                {
                    Debug.LogError("failed to get bone: " + boneName);
                }
            }
            thisRenderer.bones = boneArray;

            if (boneMap.TryGetValue(OldRootBone.name, out Transform ro))
            {
                thisRenderer.rootBone = ro; //重映射根骨骼
            }
        }
    }

    // 创建一个简单的组件，专门用于延迟更改材质
    public class DelayedMaterialChanger : MonoBehaviour
    {
        public Material material;
        public float delay = 3f;
        
        private bool materialsChanged = false;

        void Start()
        {
            Invoke("ChangeMaterials", delay);
        }

        void ChangeMaterials()
        {
            if (materialsChanged) return;
            
            // 查找所有的SkinnedMeshRenderer
            var renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            
            foreach (var renderer in renderers)
            {
                // 创建新的材质数组
                Material[] newMaterials = new Material[renderer.materials.Length];
                
                // 所有槽位都使用相同的death材质
                for (int i = 0; i < newMaterials.Length; i++)
                {
                    newMaterials[i] = material;
                }
                
                // 应用新材质
                renderer.materials = newMaterials;
            }
            
            // 也处理标准MeshRenderer
            var meshRenderers = GetComponentsInChildren<MeshRenderer>();
            
            foreach (var renderer in meshRenderers)
            {
                // 创建新的材质数组
                Material[] newMaterials = new Material[renderer.materials.Length];
                
                // 所有槽位都使用相同的death材质
                for (int i = 0; i < newMaterials.Length; i++)
                {
                    newMaterials[i] = material;
                }
                
                // 应用新材质
                renderer.materials = newMaterials;
            }
            
            materialsChanged = true;
            Debug.Log("Death materials applied to ragdoll");
        }
    }
}
