using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Examples_EasyUIColorPicker
{

    public class Rotator : MonoBehaviour
    {
        public float Speed = 100f;

        private Vector3 _rotateAngles;

        private void Start()
        {
            _rotateAngles = Random.onUnitSphere;
        }

        private void Update()
        {
            transform.Rotate(_rotateAngles * Time.deltaTime * Speed);
        }
    }

}