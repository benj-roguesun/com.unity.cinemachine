using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.TestTools;

using Cinemachine;

namespace Tests.Runtime
{
#if CINEMACHINE_UNITY_ANIMATION
    [TestFixture]
    public class StateDrivenCameraTests : CinemachineRuntimeFixtureBase
    {
        private CinemachineStateDrivenCamera m_StateDrivenCamera;
        private Animator m_Animator;
        private CmCamera m_Vcam1, m_Vcam2;

        [SetUp]
        public override void SetUp()
        {
            CreateGameObject("Camera", typeof(Camera), typeof(CinemachineBrain));

            // Create a minimal character controller
            var character = CreateGameObject("Character", typeof(Animator));
            var controller = AssetDatabase.LoadMainAssetAtPath("Packages/com.unity.cinemachine/Tests/Runtime/TestController.controller") as AnimatorController;
            character.GetComponent<Animator>().runtimeAnimatorController = controller;

            // Create a state-driven camera with two vcams 
            var stateDrivenCamera = CreateGameObject("CM StateDrivenCamera", typeof(CinemachineStateDrivenCamera)).GetComponent<CinemachineStateDrivenCamera>();
            stateDrivenCamera.AnimatedTarget = character.GetComponent<Animator>();

            var vcam1 = CreateGameObject("Vcam1", typeof(CmCamera)).GetComponent<CmCamera>();
            var vcam2 = CreateGameObject("Vcam1", typeof(CmCamera)).GetComponent<CmCamera>();
            vcam1.gameObject.transform.SetParent(stateDrivenCamera.gameObject.transform);
            vcam2.gameObject.transform.SetParent(stateDrivenCamera.gameObject.transform);

            // Map states to vcams
            stateDrivenCamera.Instructions = new[]
            {
                new CinemachineStateDrivenCamera.Instruction() {FullHash = controller.layers[0].stateMachine.states[0].GetHashCode(), Camera = vcam1},
                new CinemachineStateDrivenCamera.Instruction() {FullHash = controller.layers[0].stateMachine.states[1].GetHashCode(), Camera = vcam2}
            };

            this.m_StateDrivenCamera = stateDrivenCamera;
            m_Animator = character.GetComponent<Animator>();
            this.m_Vcam1 = vcam1;
            this.m_Vcam2 = vcam2;

            base.SetUp();
        }

        [UnityTest]
        public IEnumerator Test_StateDrivenCamera_Follows_State()
        {
            yield return null; // wait one frame

            Assert.That(m_StateDrivenCamera.LiveChild.Name, Is.EqualTo(m_Vcam1.Name));

            m_Animator.SetTrigger("DoTransitionToState2");

            yield return null; // wait one frame

            Assert.That(m_StateDrivenCamera.LiveChild.Name, Is.EqualTo(m_Vcam2.Name));

            m_Animator.SetTrigger(("DoTransitionToState1"));

            yield return null; // wait one frame

            Assert.That(m_StateDrivenCamera.LiveChild.Name, Is.EqualTo(m_Vcam1.Name));
        }
    }
#endif
}