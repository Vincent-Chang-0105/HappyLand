using System.Collections;
using System.Collections.Generic;
using AudioSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class MenuEventSystemHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected List<Selectable> Selectables = new List<Selectable>();

    [Header("Sounds")]
    [SerializeField] protected SoundData soundDataButtonHover;
    [SerializeField] protected SoundData soundDataButtonClick;    
    protected SoundBuilder soundBuilder;

    public virtual void Awake()
    {
        foreach (var selectable in Selectables)
        {
            AddSelectionListeners(selectable);
        }
        
        soundBuilder = SoundManager.Instance.CreateSoundBuilder();
    }

    public virtual void OnEnable()
    {
        for(int i = 0; i < Selectables.Count; i++)
        {
            ResetToOriginalState(Selectables[i]);
        }
    }

    public virtual void OnDisable()
    {
        CleanupTweens();
    }
    
    protected virtual void AddSelectionListeners(Selectable selectable)
    {
        //add listener
        EventTrigger trigger = selectable.gameObject.GetComponent<EventTrigger>();
        if(trigger == null)
        {
            trigger = selectable.gameObject.AddComponent<EventTrigger>();
        }

        //add SELECT event
        EventTrigger.Entry SelectEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.Select
        };
        SelectEntry.callback.AddListener(OnSelect);
        trigger.triggers.Add(SelectEntry);

        //add DESELECT event
        EventTrigger.Entry DeselectEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.Deselect
        };
        DeselectEntry.callback.AddListener(OnDeselect);
        trigger.triggers.Add(DeselectEntry);

        //add ONPOINTERENTER event
        EventTrigger.Entry PointerEnter = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        PointerEnter.callback.AddListener(OnPointerEnter);
        trigger.triggers.Add(PointerEnter);

        //add ONPOINTEREXIT event
        EventTrigger.Entry PointerExit = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        PointerExit.callback.AddListener(OnPointerExit);
        trigger.triggers.Add(PointerExit);

        // Add POINTERCLICK event
        EventTrigger.Entry PointerClick = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerClick
        };
        PointerClick.callback.AddListener(OnPointerClick);
        trigger.triggers.Add(PointerClick);
    }
    
    public void OnSelect(BaseEventData eventData)
    {
        if (eventData.selectedObject != null)
        {
            Selectable sel = eventData.selectedObject.GetComponent<Selectable>();
            if (sel != null)
            {
                HandleSelect(sel);
            }
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (eventData.selectedObject != null)
        {
            Selectable sel = eventData.selectedObject.GetComponent<Selectable>();
            if (sel != null)
            {
                HandleDeselect(sel);
            }
        }
    }

    public void OnPointerEnter(BaseEventData eventData)
    {
        PointerEventData pointerEventData = eventData as PointerEventData;
        if(pointerEventData != null)
        {
            Selectable sel = pointerEventData.pointerEnter.GetComponentInParent<Selectable>();
            if (sel == null)
            {
                sel = pointerEventData.pointerEnter.GetComponentInChildren<Selectable>(); 
            }

            pointerEventData.selectedObject = sel.gameObject;
        }
    }
    
    public void OnPointerExit(BaseEventData eventData)
    {
        PointerEventData pointerEventData = eventData as PointerEventData;
        if(pointerEventData != null)
        {
            pointerEventData.selectedObject = null;
        }
    }
    
    public void OnPointerClick(BaseEventData eventData)
    {
        if (eventData.selectedObject != null)
        {
            Selectable sel = eventData.selectedObject.GetComponent<Selectable>();
            if (sel != null)
            {
                HandleClick(sel);
            }
        }
    }

    // Abstract methods that subclasses must implement
    protected abstract void HandleSelect(Selectable selectable);
    protected abstract void HandleDeselect(Selectable selectable);
    protected abstract void HandleClick(Selectable selectable);
    protected abstract void ResetToOriginalState(Selectable selectable);
    protected abstract void CleanupTweens();
}