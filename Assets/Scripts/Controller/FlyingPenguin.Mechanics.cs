using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class FlyingPenguinController : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("撞上了");    
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Coin"))
        {
            Debug.Log("碰到了金币");
            this.maxForwardSpeed += 20f;
            maxForwardSpeed = Mathf.Min(maxForwardSpeed, 900); // 限制最大速度
            Destroy(other.gameObject);
        }
    }
}
