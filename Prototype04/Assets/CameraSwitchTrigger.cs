using Cinemachine;
using UnityEngine;


[AddComponentMenu("Camera/Camera Switch Trigger")]
[RequireComponent(typeof(Collider))]
public class CameraSwitchTrigger : MonoBehaviour
{

 [Tooltip("当玩家进入区域时激活的虚拟相机")]
    public CinemachineVirtualCamera zoneCamera;
    
    [Tooltip("相机进入区域时的优先级")]
    public int activePriority = 15;
    
    [Tooltip("相机离开区域时的优先级")]
    public int inactivePriority = 0;    
    private string playerTag = "Animal";
    
    [Tooltip("Malbers脚本中使用的相机目标变量")]
    public MalbersAnimations.Scriptables.TransformVar cameraTarget;

    [Header("调试设置")]
    [Tooltip("是否在运行时显示触发区域")]
    public bool showDebugZone = true;
    
    [Tooltip("区域颜色")]
    public Color zoneColor = new Color(0, 0.7f, 0.9f, 0.3f);
    private Collider triggerCollider;

    
    private void Start()
    {
        // 确保是触发器
        GetComponent<Collider>().isTrigger = true;
        
        // 初始化相机
        if (zoneCamera != null)
        {
            // 设置初始优先级
            zoneCamera.Priority = inactivePriority;
            
            // 确保在游戏开始时更新目标
            UpdateCameraTarget();
        }
    }
    
    private void Update()
    {
        // 持续更新相机目标
        UpdateCameraTarget();
    }
    
    // 更新相机目标
    private void UpdateCameraTarget()
    {
        if (zoneCamera != null && cameraTarget != null && cameraTarget.Value != null)
        {
            zoneCamera.Follow = cameraTarget.Value;
            zoneCamera.LookAt = cameraTarget.Value;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && zoneCamera != null)
        {
            // 简单地改变优先级，让Cinemachine处理过渡
            zoneCamera.Priority = activePriority;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) && zoneCamera != null)
        {
            // 简单地改变优先级，让Cinemachine处理过渡
            zoneCamera.Priority = inactivePriority;
        }
    }
    
    // Unity编辑器中的可视化
    // 编辑器中的可视化
    private void OnDrawGizmos()
    {
        DrawZoneGizmo();
    }
    
    // 运行时可视化
    private void OnDrawGizmosSelected()
    {
        // 在编辑器中选中时总是绘制
        DrawZoneGizmo();
    }
    
    // 自定义方法来绘制区域
    private void DrawZoneGizmo()
    {
        // 在运行时，仅当showDebugZone为true时绘制
        if (Application.isPlaying && !showDebugZone)
            return;
        
        Gizmos.color = zoneColor;
        
        // 如果没有碰撞体引用，尝试获取
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider>();
        
        if (triggerCollider != null)
        {
            // 绘制不同类型的碰撞体
            if (triggerCollider is BoxCollider)
            {
                BoxCollider box = triggerCollider as BoxCollider;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 1f);
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (triggerCollider is SphereCollider)
            {
                SphereCollider sphere = triggerCollider as SphereCollider;
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawSphere(sphere.center, sphere.radius);
                Gizmos.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 1f);
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                Gizmos.matrix = oldMatrix;
            }
            else if (triggerCollider is CapsuleCollider)
            {
                // 简化的胶囊体可视化
                Gizmos.DrawSphere(transform.position, 1.5f);
                Gizmos.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 1f);
                Gizmos.DrawWireSphere(transform.position, 1.5f);
            }
            else
            {
                // 默认可视化
                Gizmos.DrawSphere(transform.position, 1.5f);
                Gizmos.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 1f);
                Gizmos.DrawWireSphere(transform.position, 1.5f);
            }
        }
        else
        {
            // 如果没有碰撞体，绘制一个默认球体
            Gizmos.DrawSphere(transform.position, 1.5f);
        }
        
        // 显示相机图标
        Gizmos.DrawIcon(transform.position + Vector3.up, "CinemachineLogoActive", true);
        
        // 如果有相机，绘制一条线连接到相机
        if (zoneCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, zoneCamera.transform.position);
        }
    }
    
    // 公共方法用于在运行时切换调试可视化
    public void ToggleDebugZone()
    {
        showDebugZone = !showDebugZone;
    }
    
    // 公共方法用于设置调试可视化
    public void SetDebugZoneVisible(bool visible)
    {
        showDebugZone = visible;
    }
}
