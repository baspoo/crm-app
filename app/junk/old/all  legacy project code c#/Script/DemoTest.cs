using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
public class DemoTest : MonoBehaviour
{
    IEnumerator Start()
    {
 

        GameStore.WebGLService.Init();
        yield return new WaitWhile(() => !GameStore.WebGLService.IsInitialized);
  
    }


    [System.Serializable]
    public class dynamicForm
    {
        public styleData style;
        public List<formData> formDatas;
        [System.Serializable]
        public class styleData
        {
            public string header;
            public string subTitle;
            public string coverImageURL;
            public string color1;
            public string color2;
            public string submitBtnName;
        }
        [System.Serializable]
        public class formData
        {
            public string key;
            public string type;
            public string name;
            public string description;
            public string placeHolder;
            public object defaultValue;
            public string[] values;
            public int min;
            public int max;
            public bool require;
        }
    }
    /*
     {
    "style": {
        "header": "Form Name",
        "subTitle": "xxxxxxxxxxxxxxxxxxxxxxxxx",
        "coverImageURL": "https://xxxxxx",
        "color1": "#000000",
        "color2": "#000000",
        "submitBtnName": "Submit"
    },
    "formDatas": [
        {
            "key": "display_name",
            "type": "text",
            "name": "Display Name",
            "placeHolder": "Enter your display name.",
            "defaultValue": "Ritichai",
            "min": 3,
            "max": 50,
            "require": true
        },
        {
            "key": "email",
            "type": "email",
            "name": "Email",
            "placeHolder": "Enter your email",
            "defaultValue": "Ritichai@gmail.com",
            "require": true
        },
        {
            "key": "phone",
            "type": "number",
            "name": "Phone",
            "placeHolder": "00-0000-0000",
            "defaultValue": "0922574966",
            "min": 10,
            "max": 10,
            "require": true
        },
        {
            "key": "about_me",
            "type": "longtext",
            "name": "About me",
            "placeHolder": "Enter somthing your about.",
            "default": "xxxxxxxxxxxxxxxxxxxxxxx"
        },
        {
            "key": "age",
            "type": "number",
            "name": "Age",
            "placeHolder": "0",
            "defaultValue": 20,
            "require": true
        },
        {
            "key": "gender",
            "type": "dropdown",
            "name": "Gender",
            "values": [
                "Male",
                "Female",
                "Other"
            ],
            "default": "Male"
        },
        {
            "key": "fav",
            "type": "checkbox",
            "name": "Fav",
            "values": [
                "Game",
                "Music",
                "Sport",
                "Dance"
            ],
            "default": [
                "Game",
                "Music"
            ],
            "require": true
        },
        {
            "key": "transport",
            "type": "radio",
            "name": "Transport",
            "values": [
                "Car",
                "Boat",
                "Plane"
            ],
            "defaultValue": "Car",
            "require": true
        },
        {
            "key": "public_information",
            "type": "toggle",
            "name": "Public Information",
            "description": "If open public data affect other user can view your data.",
            "defaultValue": true
        },
        {
            "key": "salary",
            "type": "slider",
            "name": "Salary",
            "values": [
                "0",
                "50,000"
            ],
            "default": 15000
        }
    ]
}
     * */

    public TMPro.TextMeshProUGUI text;
    public TMPro.TMP_InputField textInput;
    public void OpenF1()
    {
        var dict = textInput.text.ToDictStringObject();





        GameStore.WebGLService.OpenIframe("form1.html", payload: dict, formCallback: (result) => {
            text.text = result;
        });
    }
    public void OpenF2()
    {
        var dict = textInput.text.ToDictStringObject();
        GameStore.WebGLService.OpenIframe("form2.html", payload: dict, formCallback: (result) => {
            text.text = result;
        });
    }
    public void OpenPDF()
    {
        GameStore.WebGLService.OpenIframe("formPDF.html", formCallback: (result) => {
            text.text = result;
        });
    }
}
