using System;
using System.Collections;
using System.Collections.Generic;
using HaptGlove;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace HexR
{
    public class HaptGloveManager : MonoBehaviour
    {
        public bool isQuest;

        public HaptGloveHandler leftHand, rightHand;
        public GameObject btIndicator_L, btIndicator_R, pumpIndicator_L, pumpIndicator_R, HexRPanel;

        private string bluetoothLog;
        public TextMeshProUGUI RightBtText, LeftBtText;
        void Start()
        {
            if (isQuest)
            {
                leftHand.isQuest = true;
                rightHand.isQuest = true;
            }
            else
            {
                leftHand.isQuest = false;
                rightHand.isQuest = false;
            }

            leftHand.onBluetoothConnected += HaptGlove_OnConnected;
            leftHand.onBluetoothConnectionFailed += HaptGlove_OnConnectedFailed;
            leftHand.onBluetoothDisconnected += HaptGlove_OnDisconnected;
            leftHand.onPumpAction += HaptGlove_OnPumpAction;

            rightHand.onBluetoothConnected += HaptGlove_OnConnected;
            rightHand.onBluetoothConnectionFailed += HaptGlove_OnConnectedFailed;
            rightHand.onBluetoothDisconnected += HaptGlove_OnDisconnected;
            rightHand.onPumpAction += HaptGlove_OnPumpAction;

            //Add all layers that you want to interact with HaptGlove
            AddHaptGloveInteractableLayer("HaptGloveInteractable");
        }


        private void HaptGlove_OnConnected(HaptGloveHandler.HandType hand)
        {
            if (hand == HaptGloveHandler.HandType.Left)
            {
                btIndicator_L.SetActive(true);
                LeftBtText.text = "Left Glove Connected";
                bluetoothLog = "Left glove connected: " + "HaptGLove " + hand.ToString();
                StartCoroutine(Pump(leftHand.GetComponent<HaptGloveHandler>()));
            }
            else if (hand == HaptGloveHandler.HandType.Right)
            {
                btIndicator_R.SetActive(true);
                RightBtText.text = "Right Glove Connected";
                bluetoothLog = "Right glove connected: " + "HaptGLove " + hand.ToString();
                StartCoroutine(Pump(rightHand.GetComponent<HaptGloveHandler>()));
            }

        }

        IEnumerator Pump(HaptGloveHandler haptGloveHandler)
        {
            // Wait for the specified delay time
            yield return new WaitForSeconds(2f);

            haptGloveHandler.AirPressureSourceControl();
        }

        private void HaptGlove_OnConnectedFailed(HaptGloveHandler.HandType hand)
        {
            if (hand == HaptGloveHandler.HandType.Left)
            {
                btIndicator_L.SetActive(false);
                LeftBtText.text = "Connection failed, try again";
                bluetoothLog = "Left glove connection failed: " + "HaptGlove " + hand.ToString();
            }
            else if (hand == HaptGloveHandler.HandType.Right)
            {
                btIndicator_R.SetActive(false);
                RightBtText.text = "Connection failed, try again";
                bluetoothLog = "Right glove connection failed: " + "HaptGlove " + hand.ToString();
            }
            HexRPanel.SetActive(true);
        }

        private void HaptGlove_OnDisconnected(HaptGloveHandler.HandType hand)
        {
            if (hand == HaptGloveHandler.HandType.Left)
            {
                btIndicator_L.SetActive(false);
                LeftBtText.text = "HexR Left Disconnected";
                bluetoothLog = "Left glove disconnected: " + "HaptGlove " + hand.ToString();
            }
            else if (hand == HaptGloveHandler.HandType.Right)
            {
                btIndicator_R.SetActive(false);
                RightBtText.text = "HexR Right Disconnected";
                bluetoothLog = "Right glove disconnected: " + "HaptGlove " + hand.ToString();
            }
            HexRPanel.SetActive(true);
        }

        private void HaptGlove_OnPumpAction(HaptGloveHandler.HandType hand, bool state)
        {
            if (hand == HaptGloveHandler.HandType.Left)
            {
                LeftBtText.text = "Left Glove Ready";
                if (state)
                    pumpIndicator_L.SetActive(true);
                else
                    pumpIndicator_L.SetActive(false);
            }
            else if (hand == HaptGloveHandler.HandType.Right)
            {
                RightBtText.text = "Right Glove Ready";
                if (state)
                    pumpIndicator_R.SetActive(true);
                else
                    pumpIndicator_R.SetActive(false);
            }
        }

        public void AddHaptGloveInteractableLayer(string layerName)
        {
            leftHand.hapticsInteratableLayers.Add(LayerMask.NameToLayer(layerName));
            rightHand.hapticsInteratableLayers.Add(LayerMask.NameToLayer(layerName));
        }

        public string[] GetHaptGloveInteractableLayer()
        {
            int[] layers = rightHand.hapticsInteratableLayers.ToArray();
            string[] layerNames = new string[layers.Length];

            for (int i = 0; i < layers.Length; i++)
            {
                layerNames[i] = LayerMask.LayerToName(layers[i]);
            }

            return layerNames;
        }
    }
}
