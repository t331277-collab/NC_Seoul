using UnityEngine;
using UnityEngine.UI;

namespace NCSeoulDebug
{
    public class Debug : MonoBehaviour
    {
        public static bool ForceInvestmentSuccessChance { get; private set; }

        [SerializeField] private Button debugButton;

        private void Awake()
        {
            BindButton();
        }

        private void OnEnable()
        {
            BindButton();
        }

        private void OnDisable()
        {
            if (debugButton != null)
            {
                debugButton.onClick.RemoveListener(EnableForceInvestmentSuccessChance);
            }
        }

        public void EnableForceInvestmentSuccessChance()
        {
            ForceInvestmentSuccessChance = true;
            StructureInvestmentState[] states = FindObjectsOfType<StructureInvestmentState>(true);
            int pendingCount = 0;
            for (int i = 0; i < states.Length; i += 1)
            {
                if (states[i] != null && states[i].hasPendingInvestment)
                {
                    states[i].pendingSuccessChance = 1f;
                    pendingCount += 1;
                }
            }

            UnityEngine.Debug.Log("Investment debug enabled. All investment success chances are forced to 100%. Pending updated=" + pendingCount + ".");
        }

        private void BindButton()
        {
            if (debugButton == null)
            {
                GameObject buttonObject = GameObject.Find("UI/DebugBtn");
                if (buttonObject != null)
                {
                    debugButton = buttonObject.GetComponent<Button>();
                }
            }

            if (debugButton != null)
            {
                debugButton.onClick.RemoveListener(EnableForceInvestmentSuccessChance);
                debugButton.onClick.AddListener(EnableForceInvestmentSuccessChance);
            }
        }
    }
}
