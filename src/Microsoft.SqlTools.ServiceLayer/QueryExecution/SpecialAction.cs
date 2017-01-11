// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    /// <summary>
    /// Class that represents a Special Action which occured by user request during the query 
    /// </summary>
    public class SpecialAction {
        
        #region Private Class variables 
        bool none;
        bool expectActualYukonXmlShowPlan;
        bool expectEstimatedYukonXmlShowPlan;

        #endregion

        /// <summary>
        /// a type of XML execution plan may be returned  
        /// </summary>
        public SpecialAction()
        {
            None = true;
            ExpectActualYukonXmlShowPlan = false;
            ExpectEstimatedYukonXmlShowPlan = false;
        }

        #region Public Functions
        /// <summary>
        /// No Special action performed 
        /// </summary>
        public bool None {
            get { return none; }
            set { 
                none = value;
                if (value)
                {
                    expectActualYukonXmlShowPlan = false;
                    expectEstimatedYukonXmlShowPlan = false;
                }
            }
        }

        /// <summary>
        /// Actual XML execution plan may be returned 
        /// </summary>
        public bool ExpectActualYukonXmlShowPlan 
        {
            get { return expectActualYukonXmlShowPlan; }
            set { this.registerSpecialAction(ref expectActualYukonXmlShowPlan, value); }
        }

        /// <summary>
        /// Estimated XML execution plan may be returned  
        /// </summary>
        public bool ExpectEstimatedYukonXmlShowPlan 
        {
            get { return expectEstimatedYukonXmlShowPlan; }
            set { this.registerSpecialAction(ref expectEstimatedYukonXmlShowPlan, value); }
        }

        /// <summary>
        /// a type of XML execution plan may be returned  
        /// </summary>
        public bool ExpectYukonXMLShowPlan 
        {
            get { return ExpectEstimatedYukonXmlShowPlan || ExpectActualYukonXmlShowPlan; }
            set 
            { 
                ExpectEstimatedYukonXmlShowPlan = value;
                ExpectActualYukonXmlShowPlan = value;
            }
        }

        /// <summary>
        /// Aggregate this special action with another one  
        /// </summary>
        public void CombineSpecialAction(SpecialAction action)
        {
            if (!action.None)
            {   
                this.None = false;
                
                if (action.ExpectActualYukonXmlShowPlan) 
                {
                    this.ExpectActualYukonXmlShowPlan = true;
                }

                if (action.ExpectEstimatedYukonXmlShowPlan) 
                {
                    this.ExpectEstimatedYukonXmlShowPlan = true;
                }
            }
        }
        
        #endregion

        #region Private Helper Functions 
        /// <summary>
        /// Check to see if all properties are false, other than none 
        /// </summary>
        private bool areAllFalse()
        {
            if (!ExpectActualYukonXmlShowPlan && !ExpectEstimatedYukonXmlShowPlan)
            {
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Helper function to turn a special action on and implement needed side effects  
        /// </summary>
        private void registerSpecialAction(ref bool state, bool change)
        {
            state = change;
            if (change) 
            {
                None = false;
            }
            else if (this.areAllFalse())
            {
                None = true;
            }
        }

        #endregion

    };
}
