using System.Collections;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace NFTPort  
{ using Internal;
    
    /// <summary>
    /// Get Transactions of an NFT
    /// </summary>
    [AddComponentMenu(PortConstants.BaseComponentMenu+PortConstants.FeatureName_Txn_Collection)]
    [ExecuteAlways]
    [HelpURL(PortConstants.Txn_Collection)]
    public class Txn_Collection : MonoBehaviour
    {
        /// <summary>
        /// Currently Supported chains for this endpoint.
        /// </summary>
        public enum Chains
        {
            ethereum,
            solana
        }
        
        public enum Type
        {
            all, mint, burn, transfer_from, transfer_to, list, buy, sell, make_bid , get_bid
        }

        #region Parameter Defines

        [SerializeField] private Chains chain;
        
            [FormerlySerializedAs("_collection_address")]
            [SerializeField]
            [Tooltip("Input Collection_id / Contract Address of the NFTs")]
            private string _collection = "Input Collection/Contract Address of the NFTs";
        

            [SerializeField] private Type _type = Type.all;

            private string RequestUriInit = "https://api.nftport.xyz/v0/transactions/nfts/";
            private string WEB_URL;
            private string _apiKey;
            private bool destroyAtEnd = false;


            private UnityAction<string> OnErrorAction;
            private UnityAction<Txn_model> OnCompleteAction;
            
            [Space(20)]
            //[Header("Called After Successful API call")]
            public UnityEvent afterSuccess;
            //[Header("Called After Error API call")]
            public UnityEvent afterError;

            [Header("Run Component when this Game Object is Set Active")]
            [SerializeField] private bool onEnable = false;
            public bool debugErrorLog = true;
            public bool debugLogRawApiResponse = false;
            
            [Header("Gets filled with data and can be referenced:")]
            public Txn_model txnModel;

        #endregion


        private void Awake()
        {
            PortUser.Initialise();
            _apiKey = PortUser.GetUserApiKey();
            
        }

        private void OnEnable()
        {
            if (onEnable & Application.isPlaying)
            {
                PortUser.SetFromOnEnable();
                Run();
            }
        }

        #region SetParams and Chain Functions

        /// <summary>
        /// Initialize creates a gameobject and assings this script as a component. This must be called if you are not refrencing the script any other way and it doesn't already exists in the scene.
        /// </summary>
        /// <param name="destroyAtEnd"> Optional bool parameter can set to false to avoid Spawned GameObject being destroyed after the Api process is complete. </param>
        public static Txn_Collection Initialize(bool destroyAtEnd = true)
            {
                var _this = new GameObject(PortConstants.FeatureName_Txn_Collection).AddComponent<Txn_Collection>();
                _this.destroyAtEnd = destroyAtEnd;
                _this.onEnable = false;
                _this.debugErrorLog = false;
                return _this;
            }

        /// <summary>
        /// Set Parameters to retrieve NFT From.  ≧◔◡◔≦ .
        /// </summary>
        /// <param name="collection"> as string - EVM</param>
        /// <param name="type"> as Type{ all, mint, burn, transfer_from, transfer_to, list, buy, sell, make_bid , get_bid}.</param>
        public Txn_Collection SetParameters(string collection = null, Type type = Type.all)
        {
            if(collection!=null)
                this._collection = collection;
            if (_type != type)
                _type = type;
            return this;
        }

        /// <summary>
        /// Blockchain from which to query NFTs.
        /// </summary>
        /// <param name="chain"> Choose from available 'Chains' enum</param>
        public Txn_Collection SetChain(Chains chain)
        {
            this.chain = chain;
            return this;
        }

        /// <summary>
        /// Action on successful API Fetch. (*^∇^)ヾ(￣▽￣*)
        /// </summary>
        /// <param name="Txn_model"> Use: .OnComplete(Txns=> txns = Txns) , where txns is of type Txn_model;</param>
        /// <returns> NFTs_OwnedByAnAccount_model.Root </returns>
        public Txn_Collection OnComplete(UnityAction<Txn_model> action)
        {
            this.OnCompleteAction = action;
            return this;
        }
        
        /// <summary>
        /// Action on Error (;•͈́༚•͈̀)(•͈́༚•͈̀;)՞༘՞༘՞
        /// </summary>
        /// <param name="UnityAction action"> string.</param>
        /// <returns> Information on Error as string text.</returns>
        public Txn_Collection OnError(UnityAction<string> action)
        {
            this.OnErrorAction = action;
            return this;
        }
            
        #endregion

        
        #region Run - API
            /// <summary>
            /// Runs the Api call and fills the corresponding model in the component on success.
            /// </summary>
            public Txn_model Run()
            {
                WEB_URL = BuildUrl();
                StopAllCoroutines();
                StartCoroutine(CallAPIProcess());
                return txnModel;
            }

            string BuildUrl()
            {
                if (chain == Chains.solana)
                {
                    WEB_URL = "https://api.nftport.xyz/v0/solana/transactions/nfts/" + _collection + "?type=" + _type.ToString();
                }
                else
                {
                    WEB_URL = RequestUriInit + _collection + "?chain=" + chain.ToString().ToLower() + "&type=" + _type.ToString();
                }
                
                if(debugErrorLog)
                    Debug.Log("Querying Transactions of Collection: " + _collection + " on " + chain);
                
                return WEB_URL;
            }
            
            IEnumerator CallAPIProcess()
            {
                //Make request
                UnityWebRequest request = UnityWebRequest.Get(WEB_URL);
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("source", PortUser.GetSource());
                request.SetRequestHeader("Authorization", _apiKey);
                
                {
                    yield return request.SendWebRequest();
                    string jsonResult = System.Text.Encoding.UTF8.GetString(request.downloadHandler.data);
                    
                    if(debugLogRawApiResponse)
                        Debug.Log(jsonResult);

                    if (request.error != null)
                    {
                        if(OnErrorAction!=null)
                            OnErrorAction($"Null data. Response code: {request.responseCode}. Result {jsonResult}");
                        if(debugErrorLog)
                            Debug.Log($"(;•͈́༚•͈̀)(•͈́༚•͈̀;)՞༘՞༘՞ Null data. Response code: {request.responseCode}. Result {jsonResult}");
                        if(afterError!=null)
                            afterError.Invoke();

                        txnModel = null;
                        //yield break;
                    }
                    else
                    {
                        //Fill Data Model from recieved class
                        txnModel = JsonConvert.DeserializeObject<Txn_model>(
                            jsonResult,
                            new JsonSerializerSettings
                            {
                            NullValueHandling = NullValueHandling.Ignore,
                            MissingMemberHandling = MissingMemberHandling.Ignore
                            });
                        
                        if(OnCompleteAction!=null)
                            OnCompleteAction.Invoke(txnModel);
                        
                        if(afterSuccess!=null)
                            afterSuccess.Invoke();
                        
                        if(debugErrorLog)
                            Debug.Log($"(*^∇^)ヾ(￣▽￣*) Success , view Txns model" );
                    }
                }
                request.Dispose();
                if(destroyAtEnd)
                    Destroy (this.gameObject);
            }
            
        #endregion
    }

}
