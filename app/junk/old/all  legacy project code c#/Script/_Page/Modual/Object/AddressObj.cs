//using UnityEngine;

//public class AddressObj : BasePool
//{
 
//    public enum Action
//    {
//        edit,
//        choose
//    }
//    public static AddressObj DisplayAddress(Transform root, CRMData.UserData.UserAddressesData address, Action? action = null, System.Action onClock = null)
//    {
//        var ui = CustomerTheme.instance.addressObj.Pool(CustomerTheme.instance.addressObj, root).GetComponent<AddressObj>();
//        ui.Label[0].AssignTextMergeName(address.FullString());
//        ui.Trans[0].SetActive(address.isDefault);
//        ui.Trans[1].SetActive(canEdit);
//        ui.Buttons[0].interactable = action != null;
//        ui.onSelect = action;
//        return ui;
//    }



//}
