using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationCallBack : MonoBehaviour
{
	








 

    public System.Action<string> CallbackState = null;
    public List<AnimTrigger> AnimTriggers;
    [System.Serializable]
    public class AnimTrigger
    {
        public string eventName;
        public UnityEngine.Events.UnityEvent onTriggered;
    }
    public void OnTriggered(string eventName)
    {
        CallbackState?.Invoke(eventName);
        var findevent = AnimTriggers.Find(x => x.eventName == eventName);
        if (findevent != null)
        {
            findevent.onTriggered?.Invoke();
        }
    }
    public void OnPlaySfx(AudioClip sfx)
    {
        sfx.Play();
    }







    Animator m_animtor;
    Animator animtor
    {
        get
        {
            if (m_animtor == null)
                m_animtor = GetComponent<Animator>();
            return m_animtor;
        }
    }
    public void PlayAnimator(string anim)
    {
        if (animtor != null)
            animtor.Play(anim);
    }
    public void SetEnableBool(string state)
    {
        if (animtor != null)
            animtor.SetBool(state, true);
    }
    public void SetDisableBool(string state)
    {
        if (animtor != null)
            animtor.SetBool(state, false);
    }
    public void SetTrigger(string state)
    {
        if (animtor != null)
            animtor.SetTrigger(state);
    }
    public void DeactiveSelf()
    {
        gameObject.SetActive(false);
    }

    Dictionary<string, float> jumpNames;
    public void SaveJump(string jumpName)
    {
        if (jumpNames == null)
            jumpNames = new Dictionary<string, float>();

        Animator animator = GetComponent<Animator>();
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0); // 0 = Base Layer
        float normalizedTime = stateInfo.normalizedTime;

        // ความยาวของคลิป (หน่วย: วินาที)
        float clipLength = stateInfo.length;

        // เวลาปัจจุบันในคลิป (jumpTime)
        float currentTime = normalizedTime * clipLength;

        jumpNames[jumpName] = currentTime;
    }
    public void JumpTo(string jumpName)
    {
        if (jumpNames != null && jumpNames.ContainsKey(jumpName))
        {
            Animator animator = GetComponent<Animator>();
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0); // 0 = Base Layer
            float normalizedTime = jumpNames[jumpName] / stateInfo.length;
            animator.Play(stateInfo.fullPathHash, 0, normalizedTime);
        }
    }
    public void Jump5ChanceTo(string jumpName)
    {
        if (5.IsPercent())
            JumpTo(jumpName);
    }
    public void Jump10ChanceTo(string jumpName)
    {
        if (10.IsPercent())
            JumpTo(jumpName);
    }
    public void Jump25ChanceTo(string jumpName)
    {
        if (25.IsPercent())
            JumpTo(jumpName);
    }
    public void Jump50ChanceTo(string jumpName)
    {
        if (50.IsPercent())
            JumpTo(jumpName);
    }
    public void Jump75ChanceTo(string jumpName)
    {
        if (75.IsPercent())
            JumpTo(jumpName);
    }

}
