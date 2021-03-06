using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ActionHandler : NetworkBehaviour {

    // Local variables only
    public GameObject itemInFocus = null;
    public GameObject itemFocusOverride = null;
    public GameObject counterInFocus = null;

    public NetworkIdentity netID;

    public bool continuousAction = false;
    public bool useButtonHeld = false;
    
    [SyncVar]
    public bool controlEnabled = true;

    [SyncVar]
    public string itemInHandsName = "";

    public GameObject itemInHands = null;

    public bool init = false;

    Vector3 velocity;
    Vector3 oneFrameAgo;
    

    // INIT
    private void Start()
    {
        gameObject.name = gameObject.name + GetComponent<NetworkIdentity>().netId;

        netID = GetComponent<NetworkIdentity>();

        if (itemInHandsName != "")
            StartCoroutine(initialize());

        if (isLocalPlayer)
        {
            UseButton.S.GetComponent<Button>().onClick.AddListener(UseAction);
            GrabButton.S.GetComponent<Button>().onClick.AddListener(GrabAction);
            
            VoteButton.S.GetComponent<Button>().onClick.AddListener(VoteAction);
            
            
            VoteManager.S.voteButton1.GetComponent<Button>().onClick.AddListener(VoteForP1);
            VoteManager.S.voteButton2.GetComponent<Button>().onClick.AddListener(VoteForP2);
            VoteManager.S.voteButton3.GetComponent<Button>().onClick.AddListener(VoteForP3);
            VoteManager.S.voteButton4.GetComponent<Button>().onClick.AddListener(VoteForP4);
            VoteManager.S.closeButton.GetComponent<Button>().onClick.AddListener(VoteCloseAction);

            EventTrigger useButtonHoldTrigger = UseButton.S.gameObject.AddComponent<EventTrigger>();
            var pointerDown = new EventTrigger.Entry();
            pointerDown.eventID = EventTriggerType.PointerDown;
            pointerDown.callback.AddListener(UseButtonDownHandler);
            useButtonHoldTrigger.triggers.Add(pointerDown);

            var pointerUp = new EventTrigger.Entry();
            pointerUp.eventID = EventTriggerType.PointerUp;
            pointerUp.callback.AddListener(UseButtonUpHandler);
            useButtonHoldTrigger.triggers.Add(pointerUp);
        }

    }

    public void UseButtonDownHandler(BaseEventData e)
    {
       
        Debug.Log("Use Held!" + Random.Range(0, 10));
        useButtonHeld = true;

    }
    
    public void UseButtonUpHandler(BaseEventData e)
    {
        Debug.Log("Use Left!" + Random.Range(0, 10));
        useButtonHeld = false;
       
    }
    
    

    private void Update()
    {
        if (!isLocalPlayer) return;
        
        if (Input.GetKeyDown(KeyCode.G) && controlEnabled)
        {
            Debug.Log("Trying to pickup.");
            GrabAction();
        }

        // To Change to suitable controls.
        if (Input.GetKeyDown(KeyCode.R)  && controlEnabled)
        {
            UseAction();
        }
        
        // Check to see if we should do single action or perform continuous action
        if (continuousAction)
        {
            if ((Input.GetKey(KeyCode.R) || useButtonHeld) && controlEnabled)
            {
                UseAction();
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.R)  && controlEnabled)
            {
                UseAction();
            }
        }
    }


    // This script waits until everything is loaded properly to initialize fully.
    IEnumerator initialize()
    {
        Debug.Log("Waiting until an item is found.");
        while (GameObject.Find(itemInHandsName) == null)
        {
            yield return null;
        }

        Debug.Log("Object found!");

        LocalGrabItem(GameObject.Find(itemInHandsName), null);
    }

    // UPDATES
    private void FixedUpdate()
    {
        // Set velocity for use with items so they can be tossed
        velocity = transform.position - oneFrameAgo;
        oneFrameAgo = transform.position;
    }

    // LOCAL ACTIONS CALLED
    public void GrabAction()
    {
        
        if (!controlEnabled) return;
        
        if (itemInHands == null)
        {

            if (itemInFocus != null)
            {
                CmdGrabItem(itemInFocus, gameObject, counterInFocus);
            }
            else if (itemFocusOverride != null)
            {
                CmdGrabItem(itemFocusOverride, gameObject, counterInFocus);
            }

        }
        else if (itemInHands != null)
        {
            CmdPlaceItem(itemInHands, itemInFocus, counterInFocus);
        }

    }

    public void UseAction()
    {
        if (!controlEnabled) return;

        if (itemInHands != null)
            if (itemInHands.GetComponent<FoodItem>().useable)
            {
                CmdUseHandItem(itemInHands, itemInFocus, counterInFocus);
                return;
            }

        if (counterInFocus != null)
        {
            CmdUseCounter(counterInFocus, gameObject, continuousAction, Time.deltaTime);
            return;
        }

    }

    public void VoteAction()
    {
        // Player triggered vote!

        GetComponent<PlayerScript>().DisableControls();
        CmdTriggerVote(true);
    }

    public void VoteCloseAction()
    {
        CmdTriggerVote(false);
    }
    
    public void VoteForP1()
    {
        Debug.Log("Vote for 1");
        CmdVoteFor(0);
        RemoveVotingButtons();
    }
    
    public void VoteForP2()
    {
        Debug.Log("Vote for 2");
        CmdVoteFor(1);
        RemoveVotingButtons();
    }
    
    public void VoteForP3()
    {
        Debug.Log("Vote for 3");
        CmdVoteFor(2);
        RemoveVotingButtons();
    }
    
    public void VoteForP4()
    {
        Debug.Log("Vote for 4");
        CmdVoteFor(3);
        RemoveVotingButtons();
    }

    [Command]
    void CmdVoteFor(int i)
    {
        Debug.Log("Vote for i on server");
        VoteManager.S.UpdateVoteCount(i);
        
    }

    [Command]
    void CmdTriggerVote(bool enable)
    {
        VoteManager.S.StartVote(enable);
    }


    void RemoveVotingButtons()
    {
       
        VoteManager.S.voteInstructionText.text = "Voting in progress... waiting for all players!";
        VoteManager.S.VoteDialogButtons.SetActive(false);
    }
    
    

    // GRAB ITEMS
    [Command]
    void CmdGrabItem(GameObject foodItem, GameObject player, GameObject counterFocus)
    {
        if (foodItem == null || foodItem.GetComponent<FoodItem>() == null) return;
        
        var f = foodItem.GetComponent<FoodItem>();

        if (counterFocus != null && f.grabbedBy == counterFocus)
        {
            counterFocus.GetComponent<CounterItem>().itemOnCounterName = "";
            counterFocus.GetComponent<CounterItem>().RpcClearCounter();
        }

        f.RpcGetGrabbed(player);
        f.grabbedByName = player.name;
        itemInHandsName = foodItem.name;
        itemInHands = foodItem;
        RpcGrabItem(foodItem, counterFocus);

    }

    [ClientRpc]
    void RpcGrabItem(GameObject foodItem, GameObject counterFocus)
    {
        LocalGrabItem(foodItem, counterFocus);
        if (foodItem == itemInFocus)
            itemInFocus = null;

    }

    void LocalGrabItem(GameObject foodItem, GameObject counterFocus)
    {
        var f = foodItem.GetComponent<FoodItem>();
        itemInHands = foodItem;

        if (f.continuousUse)
            continuousAction = true;

        if (counterFocus != null)
        {
            if (counterFocus.GetComponent<CounterItem>().itemOnCounter == foodItem)
                counterFocus.GetComponent<CounterItem>().itemOnCounter = null;
        }
            
    }


    // PLACE ITEMS
    [Command]
    void CmdPlaceItem(GameObject foodItem, GameObject itemFocus, GameObject counterFocus)
    {

        var f = foodItem.GetComponent<FoodItem>();

        if (f.dropOverride)
        {
            if (f.DropOverride(itemFocus, counterFocus))
            {
                return;
            }
        }
        Debug.Log("PlaceItem called.");

        if (itemFocus != null && itemFocus != foodItem)
        {
            Debug.Log("PlaceItem called on food container.");
            if (itemFocus.GetComponent<FoodContainer>() != null)
            {
                var c = itemFocus.GetComponent<FoodContainer>();

                if (c.CanInsertItem(foodItem))
                {
                    itemInHandsName = "";
                    f.grabbedByName = "";

                    //Running this as a command!
                    c.InsertFood(foodItem);
                    RpcClearHands();
                    return;
                }
            }

        }

        else if (counterFocus != null)
        {
            Debug.Log("Trying to place item!");
            var c = counterFocus.GetComponent<CounterItem>();

            if (c.itemOnCounter == null && c.itemOnCounter != foodItem)
            {
                f.grabbedByName = counterFocus.name;
                c.itemOnCounterName = itemInHands.name;
                itemInHandsName = "";

                if (c.isOven)
                {
                    Debug.Log("Trying to place item in oven!");
                   
                    CmdRemoveAuthority(c.GetComponent<NetworkIdentity>());
                    
                    if (CmdSetAuthority(c.GetComponent<NetworkIdentity>(), this.GetComponent<NetworkIdentity>()))
                    {
                        Debug.Log("Placing item in oven!");
                        c.PlaceItem(foodItem);
                        RpcClearHands();
                    }
                    else
                    {
                        Debug.Log("Failed to add authority");
                    }


                }
                else
                {
                    Debug.Log("Placing item in container!");
                    c.PlaceItem(foodItem);
                    RpcClearHands();
                }
                return;
            }

        }

        DropItem(foodItem);
        RpcClearHands();

    }

    //COMMAND TO DROP ITEM (ONLY RUN FROM CMD)
    void DropItem(GameObject foodItem)
    {
        var f = foodItem.GetComponent<FoodItem>();

        f.grabbedByName = "";
        itemInHandsName = "";

        //Clear the grabbed item and send the new velocity
        f.RpcClearGrabbed(velocity*40);
    }

    
    // CLEAR HANDS
    [ClientRpc]
    public void RpcClearHands()
    {
        itemInHands = null;
        continuousAction = false;
    }

    // USE ITEM
    [Command]
    void CmdUseHandItem(GameObject itemHands, GameObject itemFocus, GameObject counterFocus)
    {
        var f = itemHands.GetComponent<FoodItem>();

        // Running as a command, basically.
        f.UseItem(itemFocus, counterFocus);
    }

    // USE COUNTER
    [Command]
    void CmdUseCounter(GameObject counterFocus, GameObject player, bool cont, float deltaTime)
    {
        var c = counterFocus.GetComponent<CounterItem>();

        // Running as a command, basically.
        c.UseCounter(player, cont, deltaTime);
    }
    
    
    
    /// ASSIGN AND REMOVE CLIENT AUTHORITY///

    bool CmdSetAuthority (NetworkIdentity grabID, NetworkIdentity playerID) {
        try
        {
            return grabID.AssignClientAuthority(playerID.connectionToClient);
            
        }
        catch (Exception e)
        {
            Debug.Log("Exception while trying to assign authority: " + e.Message);
            grabID.RemoveClientAuthority();
            try
            {
                return grabID.AssignClientAuthority(playerID.connectionToClient);

            }
            catch (Exception e2)
            {
                Debug.Log("Exception while trying to assign authority: " + e2.Message);
                return false;
            }
        }
    }

    void CmdRemoveAuthority (NetworkIdentity grabID) {
        try
        {
            grabID.RemoveClientAuthority();
        }
        catch (Exception e)
        {
            
        }
    }

}
