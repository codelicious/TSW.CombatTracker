﻿//---------------------------------------------------------------------------- 
//
// <copyright file="ListCollectionView.cs" company="Microsoft">
//    Copyright (C) 2003 by Microsoft Corporation.  All rights reserved.
// </copyright> 
//
// 
// Description: ICollectionView for collections implementing IList 
//
// See spec at http://avalon/connecteddata/Specs/CollectionView.mht 
//
// History:
//  06/02/2003 : [....]   - Ported from DotNet tree
//  03/27/2004 : kenlai     - Implement IList 
//
//--------------------------------------------------------------------------- 
 
using System;
using System.Collections; 
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel; 
using System.Diagnostics;
using System.Reflection;        // ConstructorInfo 
 
using System.Windows;
 
using MS.Internal;
using MS.Internal.Data;
using MS.Utility;
 
namespace System.Windows.Data
{ 
    ///<summary> 
    /// <seealso cref="ICollectionView"/> based on and associated to <seealso cref="IList"/>.
    ///</summary> 
    public class ListCollectionView : CollectionView, IComparer, IEditableCollectionViewAddNewItem, IItemProperties
    {
        //-----------------------------------------------------
        // 
        //  Constructors
        // 
        //----------------------------------------------------- 

        #region Constructors 

        /// <summary>
        /// Constructor
        /// </summary> 
        /// <param name="list">Underlying IList</param>
        public ListCollectionView(IList list) 
            : base(list) 
        {
            _internalList = list; 

            if (InternalList.Count == 0)    // don't call virtual IsEmpty in ctor
            {
                SetCurrent(null, -1, 0); 
            }
            else 
            { 
                SetCurrent(InternalList[0], 0, 1);
            } 

            _group = new CollectionViewGroupRoot(this);
            _group.GroupDescriptionChanged += new EventHandler(OnGroupDescriptionChanged);
            ((INotifyCollectionChanged)_group).CollectionChanged += new NotifyCollectionChangedEventHandler(OnGroupChanged); 
            ((INotifyCollectionChanged)_group.GroupDescriptions).CollectionChanged += new NotifyCollectionChangedEventHandler(OnGroupByChanged);
        } 
 
        #endregion Constructors
 
        //------------------------------------------------------
        //
        //  Public Methods
        // 
        //-----------------------------------------------------
 
        #region Public Methods 

        //------------------------------------------------------ 
        #region ICollectionView

        /// <summary>
        /// Re-create the view over the associated IList 
        /// </summary>
        /// <remarks> 
        /// Any sorting and filtering will take effect during Refresh. 
        /// </remarks>
        protected override void RefreshOverride() 
        {
            lock(SyncRoot)
            {
                ClearChangeLog(); 
                if (UpdatedOutsideDispatcher)
                { 
                    ShadowCollection = new ArrayList((ICollection)SourceCollection); 
                }
            } 

            object oldCurrentItem = CurrentItem;
            int oldCurrentPosition = IsEmpty ? -1 : CurrentPosition;
            bool oldIsCurrentAfterLast = IsCurrentAfterLast; 
            bool oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;
 
            // force currency off the collection (gives user a chance to save dirty information) 
            OnCurrentChanging();
 
            IList list = UpdatedOutsideDispatcher ? ShadowCollection : (SourceCollection as IList);
            PrepareSortAndFilter(list);

            // if there's no sort/filter, just use the collection's array 
            if (!UsesLocalArray)
            { 
                _internalList = list; 
            }
            else 
            {
                _internalList = PrepareLocalArray(list);
            }
 
            PrepareGroups();
 
            if (oldIsCurrentBeforeFirst || IsEmpty) 
            {
                SetCurrent(null, -1); 
            }
            else if (oldIsCurrentAfterLast)
            {
                SetCurrent(null, InternalCount); 
            }
            else // set currency back to old current item 
            { 
                // oldCurrentItem may be null
 
                // if there are duplicates, use the position of the first matching item
                int newPosition = InternalIndexOf(oldCurrentItem);

                if (newPosition < 0) 
                {
                    // oldCurrentItem not found: move to first item 
                    object newItem; 
                    newPosition = (NewItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning) ?
                                1 : 0; 
                    if (newPosition < InternalCount && (newItem = InternalItemAt(newPosition)) != NewItemPlaceholder)
                    {
                        SetCurrent(newItem, newPosition);
                    } 
                    else
                    { 
                        SetCurrent(null, -1); 
                    }
                } 
                else
                {
                    SetCurrent(oldCurrentItem, newPosition);
                } 
            }
 
            // tell listeners everything has changed 
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
 
            OnCurrentChanged();

            if (IsCurrentAfterLast != oldIsCurrentAfterLast)
                OnPropertyChanged(IsCurrentAfterLastPropertyName); 

            if (IsCurrentBeforeFirst != oldIsCurrentBeforeFirst) 
                OnPropertyChanged(IsCurrentBeforeFirstPropertyName); 

            if (oldCurrentPosition != CurrentPosition) 
                OnPropertyChanged(CurrentPositionPropertyName);

            if (oldCurrentItem != CurrentItem)
                OnPropertyChanged(CurrentItemPropertyName); 

        } 
 
        /// <summary>
        /// Return true if the item belongs to this view.  No assumptions are 
        /// made about the item. This method will behave similarly to IList.Contains()
        /// and will do an exhaustive search through all items in this view.
        /// If the caller knows that the item belongs to the
        /// underlying collection, it is more efficient to call PassesFilter. 
        /// </summary>
        public override bool Contains(object item) 
        { 
            VerifyRefreshNotDeferred();
 
            return InternalContains(item);
        }

        /// <summary> 
        /// Move <seealso cref="CurrentItem"/> to the item at the given index.
        /// </summary> 
        /// <param name="position">Move CurrentItem to this index</param> 
        /// <returns>true if <seealso cref="CurrentItem"/> points to an item within the view.</returns>
        public override bool MoveCurrentToPosition(int position) 
        {
            VerifyRefreshNotDeferred();

            if (position < -1 || position > InternalCount) 
                throw new ArgumentOutOfRangeException("position");
 
 
            if (position != CurrentPosition || !IsCurrentInSync)
            { 
                object proposedCurrentItem = (0 <= position && position < InternalCount) ? InternalItemAt(position) : null;

                // ignore moves to the placeholder
                if (proposedCurrentItem != NewItemPlaceholder) 
                {
                    if (OKToChangeCurrent()) 
                    { 
                        bool oldIsCurrentAfterLast = IsCurrentAfterLast;
                        bool oldIsCurrentBeforeFirst = IsCurrentBeforeFirst; 

                        SetCurrent(proposedCurrentItem, position);

                        OnCurrentChanged(); 

                        // notify that the properties have changed. 
                        if (IsCurrentAfterLast != oldIsCurrentAfterLast) 
                            OnPropertyChanged(IsCurrentAfterLastPropertyName);
 
                        if (IsCurrentBeforeFirst != oldIsCurrentBeforeFirst)
                            OnPropertyChanged(IsCurrentBeforeFirstPropertyName);

                        OnPropertyChanged(CurrentPositionPropertyName); 
                        OnPropertyChanged(CurrentItemPropertyName);
                    } 
                } 
            }
 
            return IsCurrentInView;
        }

        /// <summary> 
        /// Returns true if this view really supports grouping.
        /// When this returns false, the rest of the interface is ignored. 
        /// </summary> 
        public override bool CanGroup
        { 
            get { return true; }
        }

        /// <summary> 
        /// The description of grouping, indexed by level.
        /// </summary> 
        public override ObservableCollection<GroupDescription> GroupDescriptions 
        {
            get { return _group.GroupDescriptions; } 
        }

        /// <summary>
        /// The top-level groups, constructed according to the descriptions 
        /// given in GroupDescriptions and/or GroupBySelector.
        /// </summary> 
        public override ReadOnlyObservableCollection<object> Groups 
        {
            get { return (IsGrouping) ? _group.Items : null; } 
        }

        #endregion ICollectionView
 

        /// <summary> 
        /// Return true if the item belongs to this view.  The item is assumed to belong to the 
        /// underlying DataCollection;  this method merely takes filters into account.
        /// It is commonly used during collection-changed notifications to determine if the added/removed 
        /// item requires processing.
        /// Returns true if no filter is set on collection view.
        /// </summary>
        public override bool PassesFilter(object item) 
        {
            return ActiveFilter == null || ActiveFilter(item); 
        } 

        /// <summary> Return the index where the given item belongs, or -1 if this index is unknown. 
        /// </summary>
        /// <remarks>
        /// If this method returns an index other than -1, it must always be true that
        /// view[index-1] &lt; item &lt;= view[index], where the comparisons are done via 
        /// the view's IComparer.Compare method (if any).
        /// (This method is used by a listener's (e.g. System.Windows.Controls.ItemsControl) 
        /// CollectionChanged event handler to speed up its reaction to insertion and deletion of items. 
        /// If IndexOf is  not implemented, a listener does a binary search using IComparer.Compare.)
        /// </remarks> 
        /// <param name="item">data item</param>
        public override int IndexOf(object item)
        {
            VerifyRefreshNotDeferred(); 

            return InternalIndexOf(item); 
        } 

        /// <summary> 
        /// Retrieve item at the given zero-based index in this CollectionView.
        /// </summary>
        /// <remarks>
        /// <p>The index is evaluated with any SortDescriptions or Filter being set on this CollectionView.</p> 
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"> 
        /// Thrown if index is out of range 
        /// </exception>
        public override object GetItemAt(int index) 
        {
            VerifyRefreshNotDeferred();

            return InternalItemAt(index); 
        }
 
 
        //------------------------------------------------------
        #region IComparer 

        /// <summary> Return -, 0, or +, according to whether o1 occurs before, at, or after o2 (respectively)
        /// </summary>
        /// <param name="o1">first object</param> 
        /// <param name="o2">second object</param>
        /// <remarks> 
        /// Compares items by their resp. index in the IList. 
        /// </remarks>
        int IComparer.Compare(object o1, object o2) 
        {
            return Compare(o1, o2);
        }
 
        /// <summary> Return -, 0, or +, according to whether o1 occurs before, at, or after o2 (respectively)
        /// </summary> 
        /// <param name="o1">first object</param> 
        /// <param name="o2">second object</param>
        /// <remarks> 
        /// Compares items by their resp. index in the IList.
        /// </remarks>
        protected virtual int Compare(object o1, object o2)
        { 
            if (!IsGrouping)
            { 
                if (ActiveComparer != null) 
                    return ActiveComparer.Compare(o1, o2);
 
                int i1 = InternalList.IndexOf(o1);
                int i2 = InternalList.IndexOf(o2);
                return (i1 - i2);
            } 
            else
            { 
                int i1 = InternalIndexOf(o1); 
                int i2 = InternalIndexOf(o2);
                return (i1 - i2); 
            }
        }

        #endregion IComparer 

        /// <summary> 
        /// Implementation of IEnumerable.GetEnumerator(). 
        /// This provides a way to enumerate the members of the collection
        /// without changing the currency. 
        /// </summary>
        protected override IEnumerator GetEnumerator()
        {
            VerifyRefreshNotDeferred(); 

            return InternalGetEnumerator(); 
        } 

        #endregion Public Methods 


        //-----------------------------------------------------
        // 
        //  Public Properties
        // 
        //------------------------------------------------------ 

        #region Public Properties 

        //-----------------------------------------------------
        #region ICollectionView
 
        /// <summary>
        /// Collection of Sort criteria to sort items in this view over the SourceCollection. 
        /// </summary> 
        /// <remarks>
        /// <p> 
        /// One or more sort criteria in form of <seealso cref="SortDescription"/>
        /// can be added, each specifying a property and direction to sort by.
        /// </p>
        /// </remarks> 
        public override SortDescriptionCollection SortDescriptions
        { 
            get 
            {
                if (_sort == null) 
                    SetSortDescriptions(new SortDescriptionCollection());
                return _sort;
            }
        } 

        /// <summary> 
        /// Test if this ICollectionView supports sorting before adding 
        /// to <seealso cref="SortDescriptions"/>.
        /// </summary> 
        /// <remarks>
        /// ListCollectionView does implement an IComparer based sorting.
        /// </remarks>
        public override bool CanSort 
        {
            get { return true; } 
        } 

        /// <summary> 
        /// Test if this ICollectionView supports filtering before assigning
        /// a filter callback to <seealso cref="Filter"/>.
        /// </summary>
        public override bool CanFilter 
        {
            get { return true; } 
        } 

        /// <summary> 
        /// Filter is a callback set by the consumer of the ICollectionView
        /// and used by the implementation of the ICollectionView to determine if an
        /// item is suitable for inclusion in the view.
        /// </summary> 
        /// <exception cref="NotSupportedException">
        /// Simpler implementations do not support filtering and will throw a NotSupportedException. 
        /// Use <seealso cref="CanFilter"/> property to test if filtering is supported before 
        /// assigning a non-null value.
        /// </exception> 
        public override Predicate<object> Filter
        {
            get
            { 
                return base.Filter;
            } 
            set 
            {
                if (IsAddingNew || IsEditingItem) 
                    throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringAddOrEdit, "Filter"));
                base.Filter = value;
            }
        } 

        #endregion ICollectionView 
 
        /// <summary>
        /// Set a custom comparer to sort items using an object that implements IComparer. 
        /// </summary>
        /// <remarks>
        /// Setting the Sort criteria has no immediate effect,
        /// an explicit <seealso cref="Refresh"/> call by the app is required. 
        /// Note: Setting the custom comparer object will clear previously set <seealso cref="SortDescriptions"/>.
        /// </remarks> 
        public IComparer CustomSort 
        {
            get { return _customSort; } 
            set
            {
                if (IsAddingNew || IsEditingItem)
                    throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringAddOrEdit, "CustomSort")); 
                _customSort = value;
                SetSortDescriptions(null); 
 
                RefreshOrDefer();
            } 
        }

        /// <summary>
        /// A delegate to select the group description as a function of the 
        /// parent group and its level.
        /// </summary> 
        [DefaultValue(null)] 
        public virtual GroupDescriptionSelectorCallback GroupBySelector
        { 
            get { return _group.GroupBySelector; }
            set
            {
                if (!CanGroup) 
                    throw new NotSupportedException();
                if (IsAddingNew || IsEditingItem) 
                    throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringAddOrEdit, "Grouping")); 

                _group.GroupBySelector = value; 

                RefreshOrDefer();
            }
        } 

        /// <summary> 
        /// Return the estimated number of records (or -1, meaning "don't know"). 
        /// </summary>
        public override int Count 
        {
            get
            {
                VerifyRefreshNotDeferred(); 

                return InternalCount; 
            } 
        }
 
        /// <summary>
        /// Returns true if the resulting (filtered) view is emtpy.
        /// </summary>
        public override bool IsEmpty 
        {
            get { return (InternalCount == 0); } 
        } 

        /// <summary> 
        /// Setting this to true informs the view that the list of items
        /// (after applying the sort and filter, if any) is already in the
        /// correct order for grouping.  This allows the view to use a more
        /// efficient algorithm to build the groups. 
        /// </summary>
        public bool IsDataInGroupOrder 
        { 
            get { return _group.IsDataInGroupOrder; }
            set { _group.IsDataInGroupOrder = value; } 
        }

        #endregion Public Properties
 
        #region IEditableCollectionView
 
        #region Adding new items 

        /// <summary> 
        /// Indicates whether to include a placeholder for a new item, and if so,
        /// where to put it.
        /// </summary>
        public NewItemPlaceholderPosition NewItemPlaceholderPosition 
        {
            get { return _newItemPlaceholderPosition; } 
            set 
            {
                VerifyRefreshNotDeferred(); 

                if (value != _newItemPlaceholderPosition && IsAddingNew)
                    throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringTransaction, "NewItemPlaceholderPosition", "AddNew"));
 
                NotifyCollectionChangedEventArgs args = null;
                int oldIndex=-1, newIndex=-1; 
 
                // we're adding, removing, or moving the placeholder.
                // Determine the appropriate events. 
                switch (value)
                {
                    case NewItemPlaceholderPosition.None:
                        switch (_newItemPlaceholderPosition) 
                        {
                            case NewItemPlaceholderPosition.None: 
                                break; 
                            case NewItemPlaceholderPosition.AtBeginning:
                                oldIndex = 0; 
                                args = new NotifyCollectionChangedEventArgs(
                                                NotifyCollectionChangedAction.Remove,
                                                NewItemPlaceholder,
                                                oldIndex); 
                                break;
                            case NewItemPlaceholderPosition.AtEnd: 
                                oldIndex = InternalCount - 1; 
                                args = new NotifyCollectionChangedEventArgs(
                                                NotifyCollectionChangedAction.Remove, 
                                                NewItemPlaceholder,
                                                oldIndex);
                                break;
                        } 
                        break;
 
                    case NewItemPlaceholderPosition.AtBeginning: 
                        switch (_newItemPlaceholderPosition)
                        { 
                            case NewItemPlaceholderPosition.None:
                                newIndex = 0;
                                args = new NotifyCollectionChangedEventArgs(
                                                NotifyCollectionChangedAction.Add, 
                                                NewItemPlaceholder,
                                                newIndex); 
                                break; 
                            case NewItemPlaceholderPosition.AtBeginning:
                                break; 
                            case NewItemPlaceholderPosition.AtEnd:
                                oldIndex = InternalCount - 1;
                                newIndex = 0;
                                args = new NotifyCollectionChangedEventArgs( 
                                                NotifyCollectionChangedAction.Move,
                                                NewItemPlaceholder, 
                                                newIndex, 
                                                oldIndex);
                                break; 
                        }
                        break;

                    case NewItemPlaceholderPosition.AtEnd: 
                        switch (_newItemPlaceholderPosition)
                        { 
                            case NewItemPlaceholderPosition.None: 
                                newIndex = InternalCount;
                                args = new NotifyCollectionChangedEventArgs( 
                                                NotifyCollectionChangedAction.Add,
                                                NewItemPlaceholder,
                                                newIndex);
                                break; 
                            case NewItemPlaceholderPosition.AtBeginning:
                                oldIndex = 0; 
                                newIndex = InternalCount - 1; 
                                args = new NotifyCollectionChangedEventArgs(
                                                NotifyCollectionChangedAction.Move, 
                                                NewItemPlaceholder,
                                                newIndex,
                                                oldIndex);
                                break; 
                            case NewItemPlaceholderPosition.AtEnd:
                                break; 
                        } 
                        break;
                } 

                // now make the change and raise the events
                if (args != null)
                { 
                    _newItemPlaceholderPosition = value;
 
                    if (!IsGrouping) 
                    {
                        ProcessCollectionChangedWithAdjustedIndex(args, oldIndex, newIndex); 
                    }
                    else
                    {
                        if (oldIndex >= 0) 
                        {
                            int index = (oldIndex == 0) ? 0 : _group.Items.Count - 1; 
                            _group.RemoveSpecialItem(index, NewItemPlaceholder, false /*loading*/); 
                        }
                        if (newIndex >= 0) 
                        {
                            int index = (newIndex == 0) ? 0 : _group.Items.Count;
                            _group.InsertSpecialItem(index, NewItemPlaceholder, false /*loading*/);
                        } 
                    }
 
                    OnPropertyChanged("NewItemPlaceholderPosition"); 
                }
            } 
        }

        /// <summary>
        /// Return true if the view supports <seealso cref="AddNew"/>. 
        /// </summary>
        public bool CanAddNew 
        { 
            get { return !IsEditingItem && !SourceList.IsFixedSize && CanConstructItem; }
        } 

        /// <summary>
        /// Return true if the view supports <seealso cref="AddNewItem"/>.
        /// </summary> 
        public bool CanAddNewItem
        { 
            get { return !IsEditingItem && !SourceList.IsFixedSize; } 
        }
 
        bool CanConstructItem
        {
            get
            { 
                if (!_isItemConstructorValid)
                { 
                    EnsureItemConstructor(); 
                }
 
                return (_itemConstructor != null);
            }
        }
 
        void EnsureItemConstructor()
        { 
            if (!_isItemConstructorValid) 
            {
                Type itemType = GetItemType(true); 
                if (itemType != null)
                {
                    _itemConstructor = itemType.GetConstructor(Type.EmptyTypes);
                    _isItemConstructorValid = true; 
                }
            } 
        } 

        /// <summary> 
        /// Add a new item to the underlying collection.  Returns the new item.
        /// After calling AddNew and changing the new item as desired, either
        /// <seealso cref="CommitNew"/> or <seealso cref="CancelNew"/> should be
        /// called to complete the transaction. 
        /// </summary>
        public object AddNew() 
        { 
            VerifyRefreshNotDeferred();
 
            if (IsEditingItem)
            {
                CommitEdit();   // implicitly close a previous EditItem
            } 

            CommitNew();        // implicitly close a previous AddNew 
 
            if (!CanAddNew)
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedForView, "AddNew")); 

            return AddNewCommon(_itemConstructor.Invoke(null));
        }
 
        /// <summary>
        /// Add a new item to the underlying collection.  Returns the new item. 
        /// After calling AddNewItem and changing the new item as desired, either 
        /// <seealso cref="CommitNew"/> or <seealso cref="CancelNew"/> should be
        /// called to complete the transaction. 
        /// </summary>
        public object AddNewItem(object newItem)
        {
            VerifyRefreshNotDeferred(); 

            if (IsEditingItem) 
            { 
                CommitEdit();   // implicitly close a previous EditItem
            } 

            CommitNew();        // implicitly close a previous AddNew

            if (!CanAddNewItem) 
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedForView, "AddNewItem"));
 
            return AddNewCommon(newItem); 
        }
 
        object AddNewCommon(object newItem)
        {
            _newItemIndex = -2; // this is a signal that the next Add event comes from AddNew
            int index = SourceList.Add(newItem); 

            // if the source doesn't raise collection change events, fake one 
            if (!(SourceList is INotifyCollectionChanged)) 
            {
                // the index returned by IList.Add isn't always reliable 
                if (!Object.Equals(newItem, SourceList[index]))
                {
                    index = SourceList.IndexOf(newItem);
                } 

                BeginAddNew(newItem, index); 
            } 

            Debug.Assert(_newItemIndex != -2 && Object.Equals(newItem, _newItem), "AddNew did not raise expected events"); 

            MoveCurrentTo(newItem);

            ISupportInitialize isi = newItem as ISupportInitialize; 
            if (isi != null)
            { 
                isi.BeginInit(); 
            }
 
            IEditableObject ieo = newItem as IEditableObject;
            if (ieo != null)
            {
                ieo.BeginEdit(); 
            }
 
            return newItem; 
        }
 
        // Calling IList.Add() will raise an ItemAdded event.  We handle this specially
        // to adjust the position of the new item in the view (it should be adjacent
        // to the placeholder), and cache the new item for use by the other APIs
        // related to AddNew.  This method is called from ProcessCollectionChanged. 
        void BeginAddNew(object newItem, int index)
        { 
            Debug.Assert(_newItemIndex == -2 && _newItem == NoNewItem, "unexpected call to BeginAddNew"); 

            // remember the new item and its position in the underlying list 
            SetNewItem(newItem);
            _newItemIndex = index;

            // adjust the position of the new item 
            int position = -1;
            switch (NewItemPlaceholderPosition) 
            { 
                case NewItemPlaceholderPosition.None:
                    position = UsesLocalArray ? InternalCount - 1 : _newItemIndex; 
                    break;
                case NewItemPlaceholderPosition.AtBeginning:
                    position = 1;
                    break; 
                case NewItemPlaceholderPosition.AtEnd:
                    position = InternalCount - 2; 
                    break; 
            }
 
            // raise events as if the new item appeared in the adjusted position
            ProcessCollectionChangedWithAdjustedIndex(
                                        new NotifyCollectionChangedEventArgs(
                                                NotifyCollectionChangedAction.Add, 
                                                newItem,
                                                position), 
                                        -1, position); 
        }
 
        /// <summary>
        /// Complete the transaction started by <seealso cref="AddNew"/>.  The new
        /// item remains in the collection, and the view's sort, filter, and grouping
        /// specifications (if any) are applied to the new item. 
        /// </summary>
        public void CommitNew() 
        { 
            if (IsEditingItem)
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringTransaction, "CommitNew", "EditItem")); 
            VerifyRefreshNotDeferred();

            if (_newItem == NoNewItem)
                return; 

            // grouping works differently 
            if (IsGrouping) 
            {
                CommitNewForGrouping(); 
                return;
            }

            // from the POV of view clients, the new item is moving from its 
            // position adjacent to the placeholder to its real position.
            // Remember its current position (have to do this before calling EndNew, 
            // because InternalCount depends on "adding-new" mode). 
            int fromIndex = 0;
            switch (NewItemPlaceholderPosition) 
            {
                case NewItemPlaceholderPosition.None:
                    fromIndex = UsesLocalArray ? InternalCount - 1 : _newItemIndex;
                    break; 
                case NewItemPlaceholderPosition.AtBeginning:
                    fromIndex = 1; 
                    break; 
                case NewItemPlaceholderPosition.AtEnd:
                    fromIndex = InternalCount - 2; 
                    break;
            }

            // End the AddNew transaction 
            object newItem = EndAddNew(false);
 
            // Tell the view clients what happened to the new item 
            int toIndex = AdjustBefore(NotifyCollectionChangedAction.Add, newItem, _newItemIndex);
 
            if (toIndex < 0)
            {
                // item is effectively removed (due to filter), raise a Remove event
                ProcessCollectionChangedWithAdjustedIndex( 
                            new NotifyCollectionChangedEventArgs(
                                                NotifyCollectionChangedAction.Remove, 
                                                newItem, 
                                                fromIndex),
                            fromIndex, -1); 
            }
            else if (fromIndex == toIndex)
            {
                // item isn't moving, so no events are needed.  But the item does need 
                // to be added to the local array.
                if (UsesLocalArray) 
                { 
                    if (NewItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning)
                    { 
                        --toIndex;
                    }
                    InternalList.Insert(toIndex, newItem);
                } 
            }
            else 
            { 
                // item is moving
                ProcessCollectionChangedWithAdjustedIndex( 
                            new NotifyCollectionChangedEventArgs(
                                                NotifyCollectionChangedAction.Move,
                                                newItem,
                                                toIndex, fromIndex), 
                            fromIndex, toIndex);
            } 
        } 

        void CommitNewForGrouping() 
        {
            // for grouping we cannot pretend that the new item moves to a different position,
            // since it may actually appear in several new positions (belonging to several groups).
            // Instead, we remove the item from its temporary position, then add it to the groups 
            // as if it had just been added to the underlying collection.
            int index; 
            switch (NewItemPlaceholderPosition) 
            {
                case NewItemPlaceholderPosition.None: 
                default:
                    index = _group.Items.Count - 1;
                    break;
                case NewItemPlaceholderPosition.AtBeginning: 
                    index = 1;
                    break; 
                case NewItemPlaceholderPosition.AtEnd: 
                    index = _group.Items.Count - 2;
                    break; 
            }

            // End the AddNew transaction
            int newItemIndex = _newItemIndex; 
            object newItem = EndAddNew(false);
 
            // remove item from its temporary position 
            _group.RemoveSpecialItem(index, newItem, false /*loading*/);
 
            // now pretend it just got added to the collection.  This will add it
            // to the internal list with sort/filter, and to the groups
            ProcessCollectionChanged(
                    new NotifyCollectionChangedEventArgs( 
                                NotifyCollectionChangedAction.Add,
                                newItem, 
                                newItemIndex)); 
        }
 
        /// <summary>
        /// Complete the transaction started by <seealso cref="AddNew"/>.  The new
        /// item is removed from the collection.
        /// </summary> 
        public void CancelNew()
        { 
            if (IsEditingItem) 
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringTransaction, "CancelNew", "EditItem"));
            VerifyRefreshNotDeferred(); 

            if (_newItem == NoNewItem)
                return;
 
            // remove the new item from the underlying collection.  Normally the
            // collection will raise a Remove event, which we'll handle by calling 
            // EndNew to leave AddNew mode. 
            SourceList.RemoveAt(_newItemIndex);
 
            // if the collection doesn't raise events, do the work explicitly on its behalf
            if (_newItem != NoNewItem)
            {
                int index = AdjustBefore(NotifyCollectionChangedAction.Remove, _newItem, _newItemIndex); 
                object newItem = EndAddNew(true);
 
                ProcessCollectionChangedWithAdjustedIndex( 
                            new NotifyCollectionChangedEventArgs(
                                                NotifyCollectionChangedAction.Remove, 
                                                newItem,
                                                index),
                            index, -1);
            } 
        }
 
        // Common functionality used by CommitNew, CancelNew, and when the 
        // new item is removed by Remove or Refresh.
        object EndAddNew(bool cancel) 
        {
            object newItem = _newItem;

            SetNewItem(NoNewItem);  // leave "adding-new" mode 

            IEditableObject ieo = newItem as IEditableObject; 
            if (ieo != null) 
            {
                if (cancel) 
                {
                    ieo.CancelEdit();
                }
                else 
                {
                    ieo.EndEdit(); 
                } 
            }
 
            ISupportInitialize isi = newItem as ISupportInitialize;
            if (isi != null)
            {
                isi.EndInit(); 
            }
 
            return newItem; 
        }
 
        /// <summary>
        /// Returns true if an </seealso cref="AddNew"> transaction is in progress.
        /// </summary>
        public bool IsAddingNew 
        {
            get { return (_newItem != NoNewItem); } 
        } 

        /// <summary> 
        /// When an </seealso cref="AddNew"> transaction is in progress, this property
        /// returns the new item.  Otherwise it returns null.
        /// </summary>
        public object CurrentAddItem 
        {
            get { return IsAddingNew ? _newItem : null; } 
        } 

        void SetNewItem(object item) 
        {
            if (!Object.Equals(item, _newItem))
            {
                _newItem = item; 

                OnPropertyChanged("CurrentAddItem"); 
                OnPropertyChanged("IsAddingNew"); 
                OnPropertyChanged("CanRemove");
            } 
        }

        #endregion Adding new items
 
        #region Removing items
 
        /// <summary> 
        /// Return true if the view supports <seealso cref="Remove"/> and
        /// <seealso cref="RemoveAt"/>. 
        /// </summary>
        public bool CanRemove
        {
            get { return !IsEditingItem && !IsAddingNew && !SourceList.IsFixedSize; } 
        }
 
        /// <summary> 
        /// Remove the item at the given index from the underlying collection.
        /// The index is interpreted with respect to the view (not with respect to 
        /// the underlying collection).
        /// </summary>
        public void RemoveAt(int index)
        { 
            if (IsEditingItem || IsAddingNew)
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringAddOrEdit, "RemoveAt")); 
            VerifyRefreshNotDeferred(); 

            // convert the index from "view-relative" to "list-relative" 
            int delta = (NewItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning) ? 1 : 0;
            object item = GetItemAt(index);
            if (item == CollectionView.NewItemPlaceholder)
                throw new InvalidOperationException(SR.Get(SRID.RemovingPlaceholder)); 

            int listIndex = index - delta; 
            bool raiseEvent = !(SourceList is INotifyCollectionChanged); 

            // remove the item from the list 
            if (UsesLocalArray || IsGrouping)
            {
                if (raiseEvent)
                { 
                    listIndex = SourceList.IndexOf(item);
                    SourceList.RemoveAt(listIndex); 
                } 
                else
                { 
                    SourceList.Remove(item);
                }
            }
            else 
            {
                SourceList.RemoveAt(listIndex); 
            } 

            // if the list doesn't raise CollectionChanged events, fake one 
            if (raiseEvent)
            {
                ProcessCollectionChanged(new NotifyCollectionChangedEventArgs(
                                            NotifyCollectionChangedAction.Remove, 
                                            item,
                                            listIndex)); 
            } 
        }
 
        /// <summary>
        /// Remove the given item from the underlying collection.
        /// </summary>
        public void Remove(object item) 
        {
            int index = InternalIndexOf(item); 
            if (index >= 0) 
            {
                RemoveAt(index); 
            }
        }

        #endregion Removing items 

        #region Transactional editing of an item 
 
        /// <summary>
        /// Begins an editing transaction on the given item.  The transaction is 
        /// completed by calling either <seealso cref="CommitEdit"/> or
        /// <seealso cref="CancelEdit"/>.  Any changes made to the item during
        /// the transaction are considered "pending", provided that the view supports
        /// the notion of "pending changes" for the given item. 
        /// </summary>
        public void EditItem(object item) 
        { 
            VerifyRefreshNotDeferred();
 
            if (item == NewItemPlaceholder)
                throw new ArgumentException(SR.Get(SRID.CannotEditPlaceholder), "item");

            if (IsAddingNew) 
            {
                if (Object.Equals(item, _newItem)) 
                    return;     // EditItem(newItem) is a no-op 

                CommitNew();    // implicitly close a previous AddNew 
            }

            CommitEdit();   // implicitly close a previous EditItem transaction
 
            SetEditItem(item);
 
            IEditableObject ieo = item as IEditableObject; 
            if (ieo != null)
            { 
                ieo.BeginEdit();
            }
        }
 
        /// <summary>
        /// Complete the transaction started by <seealso cref="EditItem"/>. 
        /// The pending changes (if any) to the item are committed. 
        /// </summary>
        public void CommitEdit() 
        {
            if (IsAddingNew)
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringTransaction, "CommitEdit", "AddNew"));
            VerifyRefreshNotDeferred(); 

            if (_editItem == null) 
                return; 

            object editItem = _editItem; 
            IEditableObject ieo = _editItem as IEditableObject;
            SetEditItem(null);

            if (ieo != null) 
            {
                ieo.EndEdit(); 
            } 

            // see if the item is entering or leaving the view 
            int fromIndex = InternalIndexOf(editItem);
            bool wasInView = (fromIndex >= 0);
            bool isInView = wasInView ? PassesFilter(editItem)
                                    : SourceList.Contains(editItem) && PassesFilter(editItem); 

            // editing may change the item's group names (and we can't tell whether 
            // it really did).  The best we can do is remove the item and re-insert 
            // it.
            if (IsGrouping) 
            {
                if (wasInView)
                {
                    RemoveItemFromGroups(editItem); 
                }
                if (isInView) 
                { 
                    AddItemToGroups(editItem);
                } 
                return;
            }

            // the edit may cause the item to move.  If so, report it. 
            if (UsesLocalArray)
            { 
                ArrayList list = (ArrayList)InternalList; 
                int delta = (NewItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning) ? 1 : 0;
                int toIndex = -1; 

                if (wasInView)
                {
                    if (!isInView) 
                    {
                        // the item has been effectively removed 
                        ProcessCollectionChangedWithAdjustedIndex( 
                                    new NotifyCollectionChangedEventArgs(
                                                        NotifyCollectionChangedAction.Remove, 
                                                        editItem,
                                                        fromIndex),
                                    fromIndex, -1);
                    } 
                    else if (ActiveComparer != null)
                    { 
                        // the item may have moved within the view 
                        int localIndex = fromIndex - delta;
                        if (localIndex > 0 && ActiveComparer.Compare(list[localIndex-1], editItem) > 0) 
                        {
                            // the item has moved toward the front of the list
                            toIndex = list.BinarySearch(0, localIndex, editItem, ActiveComparer);
                            if (toIndex < 0) 
                                toIndex = ~toIndex;
                        } 
                        else if (localIndex < list.Count - 1 && ActiveComparer.Compare(editItem, list[localIndex+1]) > 0) 
                        {
                            // the item has moved toward the back of the list 
                            toIndex = list.BinarySearch(localIndex+1, list.Count-localIndex-1, editItem, ActiveComparer);
                            if (toIndex < 0)
                                toIndex = ~toIndex;
                            --toIndex;      // because the item is leaving its old position 
                        }
 
                        if (toIndex >= 0) 
                        {
                            // the item has effectively moved 
                            ProcessCollectionChangedWithAdjustedIndex(
                                        new NotifyCollectionChangedEventArgs(
                                                            NotifyCollectionChangedAction.Move,
                                                            editItem, 
                                                            toIndex+delta, fromIndex),
                                        fromIndex, toIndex+delta); 
                        } 
                    }
                } 
                else if (isInView)
                {
                    // the item has effectively been added
                    toIndex = AdjustBefore(NotifyCollectionChangedAction.Add, editItem, SourceList.IndexOf(editItem)); 
                    ProcessCollectionChangedWithAdjustedIndex(
                                new NotifyCollectionChangedEventArgs( 
                                            NotifyCollectionChangedAction.Add, 
                                            editItem,
                                            toIndex+delta), 
                                -1, toIndex+delta);
                }
            }
        } 

        /// <summary> 
        /// Complete the transaction started by <seealso cref="EditItem"/>. 
        /// The pending changes (if any) to the item are discarded.
        /// </summary> 
        public void CancelEdit()
        {
            if (IsAddingNew)
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringTransaction, "CancelEdit", "AddNew")); 
            VerifyRefreshNotDeferred();
 
            if (_editItem == null) 
                return;
 
            IEditableObject ieo = _editItem as IEditableObject;
            SetEditItem(null);

            if (ieo != null) 
            {
                ieo.CancelEdit(); 
            } 
            else
                throw new InvalidOperationException(SR.Get(SRID.CancelEditNotSupported)); 
        }

        private void ImplicitlyCancelEdit()
        { 
            IEditableObject ieo = _editItem as IEditableObject;
            SetEditItem(null); 
 
            if (ieo != null)
            { 
                ieo.CancelEdit();
            }
        }
 
        /// <summary>
        /// Returns true if the view supports the notion of "pending changes" on the 
        /// current edit item.  This may vary, depending on the view and the particular 
        /// item.  For example, a view might return true if the current edit item
        /// implements <seealso cref="IEditableObject"/>, or if the view has special 
        /// knowledge about the item that it can use to support rollback of pending
        /// changes.
        /// </summary>
        public bool CanCancelEdit 
        {
            get { return (_editItem is IEditableObject); } 
        } 

        /// <summary> 
        /// Returns true if an </seealso cref="EditItem"> transaction is in progress.
        /// </summary>
        public bool IsEditingItem
        { 
            get { return (_editItem != null); }
        } 
 
        /// <summary>
        /// When an </seealso cref="EditItem"> transaction is in progress, this property 
        /// returns the affected item.  Otherwise it returns null.
        /// </summary>
        public object CurrentEditItem
        { 
            get { return _editItem; }
        } 
 
        void SetEditItem(object item)
        { 
            if (!Object.Equals(item, _editItem))
            {
                _editItem = item;
 
                OnPropertyChanged("CurrentEditItem");
                OnPropertyChanged("IsEditingItem"); 
                OnPropertyChanged("CanCancelEdit"); 
                OnPropertyChanged("CanAddNew");
                OnPropertyChanged("CanAddNewItem"); 
                OnPropertyChanged("CanRemove");
            }
        }
 
        #endregion Transactional editing of an item
 
        #endregion IEditableCollectionView 

        #region IItemProperties 

        /// <summary>
        /// Returns information about the properties available on items in the
        /// underlying collection.  This information may come from a schema, from 
        /// a type descriptor, from a representative item, or from some other source
        /// known to the view. 
        /// </summary> 
        public ReadOnlyCollection<ItemPropertyInfo> ItemProperties
        { 
            get { return GetItemProperties(); }
        }

        #endregion IItemProperties 

 
 
        //-----------------------------------------------------
        // 
        //  Protected Methods
        //
        //-----------------------------------------------------
        #region Protected Methods 

 
        /// <summary> 
        ///     Called by the the base class to notify derived class that
        ///     a CollectionChange has been posted to the message queue. 
        ///     The purpose of this notification is to allow CollectionViews to
        ///     take a snapshot of whatever information is needed at the time
        ///     of the Post (most likely the state of the Data Collection).
        /// </summary> 
        /// <param name="args">
        ///     The NotifyCollectionChangedEventArgs that is added to the change log 
        /// </param> 
        protected override void OnBeginChangeLogging(NotifyCollectionChangedEventArgs args)
        { 
            if (args == null)
                throw new ArgumentNullException("args");

            if (ShadowCollection == null || args.Action == NotifyCollectionChangedAction.Reset) 
            {
                ShadowCollection = new ArrayList((ICollection)SourceCollection); 
 
                if (!UsesLocalArray)
                { 
                    _internalList = ShadowCollection;
                }

                // the first change processed in ProcessChangeLog does 
                // not need to be applied to the ShadowCollection in
                // ProcessChangeLog because the change will already be 
                // reflected as a result of copying the Collection. 
                _applyChangeToShadow = false;
            } 
        }

        /// <summary>
        /// Handle CollectionChange events 
        /// </summary>
        protected override void ProcessCollectionChanged(NotifyCollectionChangedEventArgs args) 
        { 
            if (args == null)
                throw new ArgumentNullException("args"); 

            ValidateCollectionChangedEventArgs(args);

            // adding or replacing an item can change CanAddNew, by providing a 
            // non-null representative
            if (!_isItemConstructorValid) 
            { 
                switch (args.Action)
                { 
                    case NotifyCollectionChangedAction.Reset:
                    case NotifyCollectionChangedAction.Add:
                    case NotifyCollectionChangedAction.Replace:
                        OnPropertyChanged("CanAddNew"); 
                        break;
                } 
            } 

            int adjustedOldIndex = -1; 
            int adjustedNewIndex = -1;

            // apply the change to the shadow copy
            if (UpdatedOutsideDispatcher) 
            {
                if (_applyChangeToShadow) 
                { 
                    if (args.Action != NotifyCollectionChangedAction.Reset)
                    { 
                        if (args.Action != NotifyCollectionChangedAction.Remove && args.NewStartingIndex < 0
                            || args.Action != NotifyCollectionChangedAction.Add && args.OldStartingIndex < 0)
                        {
                            Debug.Assert(false, "Cannot update collection view from outside UIContext without index in event args"); 
                            return;     //
                        } 
                        else 
                        {
                            AdjustShadowCopy(args); 
                        }
                    }
                }
                _applyChangeToShadow = true; 
            }
 
            // If the Action is Reset then we do a Refresh. 
            if (args.Action == NotifyCollectionChangedAction.Reset)
            { 
                // implicitly cancel EditItem transactions
                if (IsEditingItem)
                {
                    ImplicitlyCancelEdit(); 
                }
 
                // adjust AddNew transactions, depending on whether the new item 
                // survived the Reset
                if (IsAddingNew) 
                {
                    _newItemIndex = SourceList.IndexOf(_newItem);
                    if (_newItemIndex < 0)
                    { 
                        EndAddNew(true);
                    } 
                } 

                RefreshOrDefer(); 
                return; // the Refresh raises collection change event, so there's nothing left to do
            }

            if (args.Action == NotifyCollectionChangedAction.Add && _newItemIndex == -2) 
            {
                // The Add event came from AddNew. 
                BeginAddNew(args.NewItems[0], args.NewStartingIndex); 
                return;
            } 

            // If the Action is one that can be expected to have a valid NewItems[0] and NewStartingIndex then
            // adjust the index for filtering and sorting.
            if (args.Action != NotifyCollectionChangedAction.Remove) 
            {
                adjustedNewIndex = AdjustBefore(NotifyCollectionChangedAction.Add, args.NewItems[0], args.NewStartingIndex); 
            } 

            // If the Action is one that can be expected to have a valid OldItems[0] and OldStartingIndex then 
            // adjust the index for filtering and sorting.
            if (args.Action != NotifyCollectionChangedAction.Add)
            {
                adjustedOldIndex = AdjustBefore(NotifyCollectionChangedAction.Remove, args.OldItems[0], args.OldStartingIndex); 

                // the new index needs further adjustment if the action removes (or moves) 
                // something before it 
                if (UsesLocalArray && adjustedOldIndex >= 0 && adjustedOldIndex < adjustedNewIndex)
                { 
                    -- adjustedNewIndex;
                }
            }
 
            // handle interaction with AddNew and EditItem
            switch (args.Action) 
            { 
                case NotifyCollectionChangedAction.Add:
                    if (args.NewStartingIndex <= _newItemIndex) 
                    {
                        ++ _newItemIndex;
                    }
                    break; 

                case NotifyCollectionChangedAction.Remove: 
                    if (args.OldStartingIndex < _newItemIndex) 
                    {
                        -- _newItemIndex; 
                    }

                    // implicitly cancel AddNew and/or EditItem transactions if the relevant item is removed
                    object item = args.OldItems[0]; 

                    if (item == CurrentEditItem) 
                    { 
                        ImplicitlyCancelEdit();
                    } 
                    else if (item == CurrentAddItem)
                    {
                        EndAddNew(true);
                    } 
                    break;
 
                case NotifyCollectionChangedAction.Move: 
                    if (args.OldStartingIndex < _newItemIndex && _newItemIndex < args.NewStartingIndex)
                    { 
                        -- _newItemIndex;
                    }
                    else if (args.NewStartingIndex <= _newItemIndex && _newItemIndex < args.OldStartingIndex)
                    { 
                        ++ _newItemIndex;
                    } 
                    break; 
            }
 
            ProcessCollectionChangedWithAdjustedIndex(args, adjustedOldIndex, adjustedNewIndex);
        }

        void ProcessCollectionChangedWithAdjustedIndex(NotifyCollectionChangedEventArgs args, int adjustedOldIndex, int adjustedNewIndex) 
        {
            // Finding out the effective Action after filtering and sorting. 
            // 
            NotifyCollectionChangedAction effectiveAction = args.Action;
            if (adjustedOldIndex == adjustedNewIndex && adjustedOldIndex >= 0) 
            {
                effectiveAction = NotifyCollectionChangedAction.Replace;
            }
            else if (adjustedOldIndex == -1) // old index is unknown 
            {
                // we weren't told the old index, but it may have been in the view. 
                if (adjustedNewIndex < 0) 
                {
                    // The new item will not be in the filtered view, 
                    // so an Add is a no-op and anything else is a Remove.
                    if (args.Action == NotifyCollectionChangedAction.Add)
                        return;
                    effectiveAction = NotifyCollectionChangedAction.Remove; 
                }
            } 
            else if (adjustedOldIndex < -1) // old item is known to be NOT in filtered view 
            {
                if (adjustedNewIndex < 0) 
                {
                    // since the old item wasn't in the filtered view, and the new
                    // item would not be in the filtered view, this is a no-op.
                    return; 
                }
                else 
                { 
                    effectiveAction = NotifyCollectionChangedAction.Add;
                } 
            }
            else // old item was in view
            {
                if (adjustedNewIndex < 0) 
                {
                    effectiveAction = NotifyCollectionChangedAction.Remove; 
                } 
                else
                { 
                    effectiveAction = NotifyCollectionChangedAction.Move;
                }
            }
 
            int delta = IsGrouping ? 0 :
                        (NewItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning) ? 
                        (IsAddingNew ? 2 : 1) : 0; 

            int originalCurrentPosition = CurrentPosition; 
            int oldCurrentPosition = CurrentPosition;
            object oldCurrentItem = CurrentItem;
            bool oldIsCurrentAfterLast = IsCurrentAfterLast;
            bool oldIsCurrentBeforeFirst = IsCurrentBeforeFirst; 

            // in the case of a replace that has a new adjustedPosition 
            // (likely caused by sorting), the only way to effectively communicate 
            // this change is through raising Remove followed by Insert.
            NotifyCollectionChangedEventArgs args2 = null; 

            switch (effectiveAction)
            {
                case NotifyCollectionChangedAction.Add: 
                    // insert into private view
                    // (unless it's a special item - placeholder or new item) 
                    if (UsesLocalArray && NewItemPlaceholder != args.NewItems[0] && 
                            (!IsAddingNew || !Object.Equals(_newItem, args.NewItems[0])))
                    { 
                        InternalList.Insert(adjustedNewIndex - delta, args.NewItems[0]);
                    }

                    if (!IsGrouping) 
                    {
                        AdjustCurrencyForAdd(adjustedNewIndex); 
                        args = new NotifyCollectionChangedEventArgs(effectiveAction, args.NewItems[0], adjustedNewIndex); 
                    }
                    else 
                    {
                        AddItemToGroups(args.NewItems[0]);
                    }
 
                    break;
 
                case NotifyCollectionChangedAction.Remove: 
                    // remove from private view, unless it's not there to start with
                    // (e.g. when CommitNew is applied to an item that fails the filter) 
                    if (UsesLocalArray)
                    {
                        int localOldIndex = adjustedOldIndex - delta;
 
                        if (localOldIndex < InternalList.Count &&
                            Object.Equals(InternalList[localOldIndex], args.OldItems[0])) 
                        { 
                            InternalList.RemoveAt(localOldIndex);
                        } 
                    }

                    if (!IsGrouping)
                    { 
                        AdjustCurrencyForRemove(adjustedOldIndex);
                        args = new NotifyCollectionChangedEventArgs(effectiveAction, args.OldItems[0], adjustedOldIndex); 
                    } 
                    else
                    { 
                        RemoveItemFromGroups(args.OldItems[0]);
                    }

                    break; 
                case NotifyCollectionChangedAction.Replace:
                    // replace item in private view 
                    if (UsesLocalArray) 
                    {
                        InternalList[adjustedOldIndex - delta] = args.NewItems[0]; 
                    }

                    if (!IsGrouping)
                    { 
                        AdjustCurrencyForReplace(adjustedOldIndex);
                        args = new NotifyCollectionChangedEventArgs(effectiveAction, args.NewItems[0], args.OldItems[0], adjustedOldIndex); 
                    } 
                    else
                    { 
                        RemoveItemFromGroups(args.OldItems[0]);
                        AddItemToGroups(args.NewItems[0]);
                    }
 
                    break;
 
                case NotifyCollectionChangedAction.Move: 
                    // remove from private view
 
                    bool simpleMove = args.OldItems[0] == args.NewItems[0];

                    if (UsesLocalArray)
                    { 
                        int localOldIndex = adjustedOldIndex - delta;
                        int localNewIndex = adjustedNewIndex - delta; 
 
                        // remove the item from its old position, unless it's not there
                        // (which happens when the item is the object of CommitNew) 
                        if (localOldIndex < InternalList.Count &&
                            Object.Equals(InternalList[localOldIndex], args.OldItems[0]))
                        {
                            InternalList.RemoveAt(localOldIndex); 
                        }
 
                        // put the item into its new position, unless it's special 
                        if (NewItemPlaceholder != args.NewItems[0])
                        { 
                            InternalList.Insert(localNewIndex, args.NewItems[0]);
                        }
                    }
 
                    if (!IsGrouping)
                    { 
                        AdjustCurrencyForMove(adjustedOldIndex, adjustedNewIndex); 

                        if (simpleMove) 
                        {
                            // simple move
                            args = new NotifyCollectionChangedEventArgs(effectiveAction, args.OldItems[0], adjustedNewIndex, adjustedOldIndex);
                        } 
                        else
                        { 
                            // move/replace 
                            args2 = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, args.NewItems, adjustedNewIndex);
                            args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, args.OldItems, adjustedOldIndex); 
                        }
                    }
                    else
                    { 
                        if (!simpleMove)
                        { 
                            RemoveItemFromGroups(args.OldItems[0]); 
                            AddItemToGroups(args.NewItems[0]);
                        } 
                    }
                    break;
                default:
                    Invariant.Assert(false, SR.Get(SRID.UnexpectedCollectionChangeAction, effectiveAction)); 
                    break;
            } 
 
            // remember whether scalar properties of the view have changed.
            // They may change again during the collection change event, so we 
            // need to do the test before raising that event.
            bool afterLastHasChanged = (IsCurrentAfterLast != oldIsCurrentAfterLast);
            bool beforeFirstHasChanged = (IsCurrentBeforeFirst != oldIsCurrentBeforeFirst);
            bool currentPositionHasChanged = (CurrentPosition != oldCurrentPosition); 
            bool currentItemHasChanged = (CurrentItem != oldCurrentItem);
 
            // take a new snapshot of the scalar properties, so that we can detect 
            // changes made during the collection change event
            oldIsCurrentAfterLast = IsCurrentAfterLast; 
            oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;
            oldCurrentPosition = CurrentPosition;
            oldCurrentItem = CurrentItem;
 
            // base class will raise an event to our listeners
            if (!IsGrouping) 
            { 
                // we've already returned if (args.Action == NotifyCollectionChangedAction.Reset) above
                OnCollectionChanged(args); 
                if (args2 != null)
                    OnCollectionChanged(args2);

                // Any scalar properties that changed don't need a further notification, 
                // but do need a new snapshot
                if (IsCurrentAfterLast != oldIsCurrentAfterLast) 
                { 
                    afterLastHasChanged = false;
                    oldIsCurrentAfterLast = IsCurrentAfterLast; 
                }
                if (IsCurrentBeforeFirst != oldIsCurrentBeforeFirst)
                {
                    beforeFirstHasChanged = false; 
                    oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;
                } 
                if (CurrentPosition != oldCurrentPosition) 
                {
                    currentPositionHasChanged = false; 
                    oldCurrentPosition = CurrentPosition;
                }
                if (CurrentItem != oldCurrentItem)
                { 
                    currentItemHasChanged = false;
                    oldCurrentItem = CurrentItem; 
                } 

            } 

            // currency has to change after firing the deletion event,
            // so event handlers have the right picture
            if (_currentElementWasRemoved) 
            {
                MoveCurrencyOffDeletedElement(originalCurrentPosition); 
 
                // changes to the scalar properties need notification
                afterLastHasChanged = afterLastHasChanged || (IsCurrentAfterLast != oldIsCurrentAfterLast); 
                beforeFirstHasChanged = beforeFirstHasChanged || (IsCurrentBeforeFirst != oldIsCurrentBeforeFirst);
                currentPositionHasChanged = currentPositionHasChanged || (CurrentPosition != oldCurrentPosition);
                currentItemHasChanged = currentItemHasChanged || (CurrentItem != oldCurrentItem);
            } 

            // notify that the properties have changed.  We may end up doing 
            // double notification for properties that change during the collection 
            // change event, but that's not harmful.  Detecting the double change
            // is more trouble than it's worth. 
            if (afterLastHasChanged)
                OnPropertyChanged(IsCurrentAfterLastPropertyName);

            if (beforeFirstHasChanged) 
                OnPropertyChanged(IsCurrentBeforeFirstPropertyName);
 
            if (currentPositionHasChanged) 
                OnPropertyChanged(CurrentPositionPropertyName);
 
            if (currentItemHasChanged)
                OnPropertyChanged(CurrentItemPropertyName);

        } 

        /// <summary> 
        /// Return index of item in the internal list. 
        /// </summary>
        protected int InternalIndexOf(object item) 
        {
            if (IsGrouping)
            {
                return _group.LeafIndexOf(item); 
            }
 
            if (item == NewItemPlaceholder) 
            {
                switch (NewItemPlaceholderPosition) 
                {
                    case NewItemPlaceholderPosition.None:
                        return -1;
 
                    case NewItemPlaceholderPosition.AtBeginning:
                        return 0; 
 
                    case NewItemPlaceholderPosition.AtEnd:
                        return InternalCount - 1; 
                }
            }
            else if (IsAddingNew && Object.Equals(item, _newItem))
            { 
                switch (NewItemPlaceholderPosition)
                { 
                    case NewItemPlaceholderPosition.None: 
                        if (UsesLocalArray)
                        { 
                            return InternalCount - 1;
                        }
                        break;
 
                    case NewItemPlaceholderPosition.AtBeginning:
                        return 1; 
 
                    case NewItemPlaceholderPosition.AtEnd:
                        return InternalCount - 2; 
                }
            }

            int index = InternalList.IndexOf(item); 

            if (NewItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning && index >= 0) 
            { 
                index += IsAddingNew ? 2 : 1;
            } 

            return index;
        }
 
        /// <summary>
        /// Return item at the given index in the internal list. 
        /// </summary> 
        protected object InternalItemAt(int index)
        { 
            if (IsGrouping)
            {
                return _group.LeafAt(index);
            } 

            switch (NewItemPlaceholderPosition) 
            { 
                case NewItemPlaceholderPosition.None:
                    if (IsAddingNew && UsesLocalArray) 
                    {
                        if (index == InternalCount - 1)
                            return _newItem;
                    } 
                    break;
 
                case NewItemPlaceholderPosition.AtBeginning: 
                    if (index == 0)
                        return NewItemPlaceholder; 
                    --index;

                    if (IsAddingNew)
                    { 
                        if (index == 0)
                            return _newItem; 
 
                        if (UsesLocalArray || index <= _newItemIndex)
                        { 
                            --index;
                        }
                    }
                    break; 

                case NewItemPlaceholderPosition.AtEnd: 
                    if (index == InternalCount - 1) 
                        return NewItemPlaceholder;
                    if (IsAddingNew) 
                    {
                        if (index == InternalCount-2)
                            return _newItem;
                        if (!UsesLocalArray && index >= _newItemIndex) 
                            ++index;
                    } 
                    break; 
            }
 
            return InternalList[index];
        }

        /// <summary> 
        /// Return true if internal list contains the item.
        /// </summary> 
        protected bool InternalContains(object item) 
        {
            if (item == NewItemPlaceholder) 
                return (NewItemPlaceholderPosition != NewItemPlaceholderPosition.None);

            return (!IsGrouping) ? InternalList.Contains(item) : (_group.LeafIndexOf(item) >= 0);
        } 

        /// <summary> 
        /// Return an enumerator for the internal list. 
        /// </summary>
        protected IEnumerator InternalGetEnumerator() 
        {
            if (!IsGrouping)
            {
                return new PlaceholderAwareEnumerator(this, InternalList.GetEnumerator(), NewItemPlaceholderPosition, _newItem); 
            }
            else 
            { 
                return _group.GetLeafEnumerator();
            } 
        }

        /// <summary>
        /// True if a private copy of the data is needed for sorting and filtering 
        /// </summary>
        protected bool UsesLocalArray 
        { 
            get { return ActiveComparer != null || ActiveFilter != null; }
        } 

        /// <summary>
        /// Protected accessor to private _internalList field.
        /// </summary> 
        protected IList InternalList
        { 
            get { return _internalList; } 
        }
 
        /// <summary>
        /// Protected accessor to private _activeComparer field.
        /// </summary>
        protected IComparer ActiveComparer 
        {
            get { return _activeComparer; } 
            set 
            {
                _activeComparer = value; 
            }
        }

        /// <summary> 
        /// Protected accessor to private _activeFilter field.
        /// </summary> 
        protected Predicate<object> ActiveFilter 
        {
            get { return _activeFilter; } 
            set { _activeFilter = value; }
        }

        /// <summary> 
        /// Protected accessor to _isGrouping field.
        /// </summary> 
        protected bool IsGrouping 
        {
            get { return _isGrouping; } 
        }

        /// <summary>
        /// Protected accessor to private count. 
        /// </summary>
        protected int InternalCount 
        { 
            get
            { 
                if (IsGrouping)
                    return _group.ItemCount;

                int delta = (NewItemPlaceholderPosition == NewItemPlaceholderPosition.None) ? 0 : 1; 
                if (UsesLocalArray && IsAddingNew)
                    ++delta; 
 
                return delta + InternalList.Count;
            } 
        }

        #endregion Protected Methods
 
        //------------------------------------------------------
        // 
        //  Internal Methods 
        //
        //----------------------------------------------------- 

        #region Internal Methods

        /// <summary> 
        ///     Contains a snapshot of the ICollectionView.SourceCollection
        ///     at the time that a change notification is posted. 
        ///     This is done in OnBeginChangeLogging. 
        /// </summary>
        internal ArrayList ShadowCollection 
        {
            get { return _shadowCollection; }
            set { _shadowCollection = value; }
        } 

        // 
 

        internal void AdjustShadowCopy(NotifyCollectionChangedEventArgs e) 
        {
            int tempIndex;

            switch (e.Action) 
            {
                case NotifyCollectionChangedAction.Add: 
                    if (e.NewStartingIndex > _unknownIndex) 
                    {
                        ShadowCollection.Insert(e.NewStartingIndex, e.NewItems[0]); 
                    }
                    else
                    {
                        ShadowCollection.Add(e.NewItems[0]); 
                    }
                    break; 
                case NotifyCollectionChangedAction.Remove: 
                    if (e.OldStartingIndex > _unknownIndex)
                    { 
                        ShadowCollection.RemoveAt(e.OldStartingIndex);
                    }
                    else
                    { 
                        ShadowCollection.Remove(e.OldItems[0]);
                    } 
                    break; 
                case NotifyCollectionChangedAction.Replace:
                    if (e.OldStartingIndex > _unknownIndex) 
                    {
                        ShadowCollection[e.OldStartingIndex] = e.NewItems[0];
                    }
                    else 
                    {
                        // allow the ShadowCollection to throw the IndexOutOfRangeException 
                        // if the item is not found. 
                        tempIndex = ShadowCollection.IndexOf(e.OldItems[0]);
                        ShadowCollection[e.OldStartingIndex] = e.NewItems[0]; 
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex > _unknownIndex) 
                    {
                        ShadowCollection.RemoveAt(e.OldStartingIndex); 
                    } 
                    else
                    { 
                        ShadowCollection.Remove(e.OldItems[0]);
                        ShadowCollection.Insert(e.NewStartingIndex, e.NewItems[0]);
                    }
                    break; 

                default: 
                    throw new NotSupportedException(SR.Get(SRID.UnexpectedCollectionChangeAction, e.Action)); 

            } 
        }

        // returns true if this ListCollectionView has sort descriptions,
        // without tripping off lazy creation of .SortDescriptions collection 
        internal bool HasSortDescriptions
        { 
            get { return ((_sort != null) && (_sort.Count > 0)); } 
        }
 
        #endregion Internal Methods


        #region Private Properties 

        //------------------------------------------------------ 
        // 
        //  Private Properties
        // 
        //------------------------------------------------------

        // true if CurrentPosition points to item within view
        private bool IsCurrentInView 
        {
            get { return (0 <= CurrentPosition && CurrentPosition < InternalCount); } 
        } 

        // can the group name(s) for an item change after we've grouped the item? 
        private bool CanGroupNamesChange
        {
            // There's no way we can deduce this - the app has to tell us.
            // If this is true, removing a grouped item is quite difficult. 
            // We cannot rely on its group names to tell us which group we inserted
            // it into (they may have been different at insertion time), so we 
            // have to do a linear search. 
            get { return true; }
        } 

        private IList SourceList
        {
            get { return SourceCollection as IList; } 
        }
 
        #endregion Private Properties 

 
        //-----------------------------------------------------
        //
        //  Private Methods
        // 
        //------------------------------------------------------
 
        #region Private Methods 

        private void ValidateCollectionChangedEventArgs(NotifyCollectionChangedEventArgs e) 
        {

            switch (e.Action)
            { 
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems.Count != 1) 
                        throw new NotSupportedException(SR.Get(SRID.RangeActionsNotSupported)); 
                    break;
 
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems.Count != 1)
                        throw new NotSupportedException(SR.Get(SRID.RangeActionsNotSupported));
                    break; 

                case NotifyCollectionChangedAction.Replace: 
                    if (e.NewItems.Count != 1 || e.OldItems.Count != 1) 
                        throw new NotSupportedException(SR.Get(SRID.RangeActionsNotSupported));
                    break; 

                case NotifyCollectionChangedAction.Move:
                    if (e.NewItems.Count != 1)
                        throw new NotSupportedException(SR.Get(SRID.RangeActionsNotSupported)); 
                    if (e.NewStartingIndex < 0)
                        throw new InvalidOperationException(SR.Get(SRID.CannotMoveToUnknownPosition)); 
                    break; 

                case NotifyCollectionChangedAction.Reset: 
                    break;

                default:
                    throw new NotSupportedException(SR.Get(SRID.UnexpectedCollectionChangeAction, e.Action)); 
            }
        } 
 

        /// <summary> 
        /// Create, filter and sort the local index array.
        /// called from Refresh(), override in derived classes as needed.
        /// </summary>
        /// <param name="list">new ILIst to associate this view with</param> 
        /// <returns>new local array to use for this view</returns>
        private IList PrepareLocalArray(IList list) 
        { 
            if (list == null)
                throw new ArgumentNullException("list"); 

            // filter the collection's array into the local array
            ArrayList al;
 
            if (ActiveFilter == null)
            { 
                al = new ArrayList(list); 

                if (IsAddingNew) 
                {
                    al.RemoveAt(_newItemIndex);
                }
            } 
            else
            { 
                al = new ArrayList(list.Count);       // 
                for (int k = 0; k < list.Count; ++k)
                { 
                    if (ActiveFilter(list[k]) && !(IsAddingNew && k == _newItemIndex))
                        al.Add(list[k]);
                }
            } 

            // sort the local array 
            if (ActiveComparer != null) 
            {
                SortFieldComparer.SortHelper(al, ActiveComparer); 
            }

            return al;
        } 

        private void MoveCurrencyOffDeletedElement(int oldCurrentPosition) 
        { 
            int lastPosition = InternalCount - 1;   // OK if last is -1
            // if position falls beyond last position, move back to last position 
            int newPosition = (oldCurrentPosition < lastPosition) ? oldCurrentPosition : lastPosition;

            // reset this to false before raising events to avoid problems in re-entrancy
            _currentElementWasRemoved = false; 

            OnCurrentChanging(); 
 
            if (newPosition < 0)
                SetCurrent(null, newPosition); 
            else
                SetCurrent(InternalItemAt(newPosition), newPosition);

            OnCurrentChanged(); 
        }
 
        // Convert the collection's index to an index into the view. 
        // Return -1 if the index is unknown or moot (Reset events).
        // Return -2 if the event doesn't apply to this view. 
        private int AdjustBefore (NotifyCollectionChangedAction action, object item, int index)
        {
            // index is not relevant to Reset events
            if (action == NotifyCollectionChangedAction.Reset) 
                return -1;
 
            if (item == NewItemPlaceholder) 
            {
                return (NewItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning) 
                        ? 0 : InternalCount - 1;
            }
            else if (IsAddingNew && NewItemPlaceholderPosition != NewItemPlaceholderPosition.None &&
                        Object.Equals(item, _newItem)) 
            {
                // we should only get here when removing the AddNew item - i.e. from CancelNew - 
                // and only when the placeholder is active. 
                // In that case the item's index in the view is 1 when the placeholder
                // is AtBeginning, and just before the placeholder when it's AtEnd. 
                // The numerical value for the latter case dependds on whether there's
                // a sort/filter or not, i.e. whether we're using a local array.  That's
                // because the item has already been removed from the collection, but
                // not from the local array.  (DDB 201860) 
                return (NewItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning)
                        ? 1 : UsesLocalArray ? InternalCount - 2 : index; 
            } 

            int delta = IsGrouping ? 0 : 
                        (NewItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning)
                        ? (IsAddingNew ? 2 : 1) : 0;
            IList ilFull = (UpdatedOutsideDispatcher ? ShadowCollection : SourceCollection) as IList;
 
            // validate input
            if (index < -1 || index > ilFull.Count) 
                throw new InvalidOperationException(SR.Get(SRID.CollectionChangeIndexOutOfRange, index, ilFull.Count)); 

            if (action == NotifyCollectionChangedAction.Add) 
            {
                if (index >= 0)
                {
                    if (!Object.Equals(item, ilFull[index])) 
                        throw new InvalidOperationException(SR.Get(SRID.AddedItemNotAtIndex, index));
                } 
                else 
                {
                    // event didn't specify index - determine it the hard way 
                    index = ilFull.IndexOf(item);
                    if (index < 0)
                        throw new InvalidOperationException(SR.Get(SRID.AddedItemNotInCollection));
                } 
            }
 
            // if there's no sort or filter, use the index into the full array 
            if (!UsesLocalArray)
            { 
                if (IsAddingNew)
                {
                    if (index > _newItemIndex)
                    { 
                        --index;        // the new item has been artificially moved elsewhere
                    } 
                } 

                return index + delta; 
            }

            if (action == NotifyCollectionChangedAction.Add)
            { 
                // if the item isn't in the filter, return -2
                if (!this.PassesFilter(item)) 
                    return -2; 

                // search the local array 
                ArrayList al = InternalList as ArrayList;
                if (al == null)
                {
                    index = -1; 
                }
                else if (ActiveComparer != null) 
                { 
                    // if there's a sort order, use binary search
                    index = al.BinarySearch(item, ActiveComparer); 
                    if (index < 0)
                        index = ~index;
                }
                else 
                {
                    // otherwise, do a linear search of the full array, advancing 
                    // localIndex past elements that appear in the local array, 
                    // until either (a) reaching the position of the item in the
                    // full array, or (b) falling off the end of the local array. 
                    // localIndex is now the desired index.
                    // One small wrinkle:  we have to ignore the target item in
                    // the local array (this arises in a Move event).
                    int fullIndex=0, localIndex=0; 

                    while (fullIndex < index && localIndex < al.Count) 
                    { 
                        if (Object.Equals(ilFull[fullIndex], al[localIndex]))
                        { 
                            // match - current item passes filter.  Skip it.
                            ++fullIndex;
                            ++localIndex;
                        } 
                        else if (Object.Equals(item, al[localIndex]))
                        { 
                            // skip over an unmatched copy of the target item 
                            // (this arises in a Move event)
                            ++localIndex; 
                        }
                        else
                        {
                            // no match - current item fails filter.  Ignore it. 
                            ++fullIndex;
                        } 
                    } 

                    index = localIndex; 
                }
            }
            else if (action == NotifyCollectionChangedAction.Remove)
            { 
                if (!IsAddingNew || item != _newItem)
                { 
                    // a deleted item should already be in the local array 
                    index = InternalList.IndexOf(item);
 
                    // but may not be, if it was already filtered out (can't use
                    // PassesFilter here, because the item could have changed
                    // while it was out of our sight)
                    if (index < 0) 
                        return -2;
                } 
                else 
                {
                    // the new item is in a special position 
                    switch (NewItemPlaceholderPosition)
                    {
                        case NewItemPlaceholderPosition.None:
                            return InternalCount - 1; 
                        case NewItemPlaceholderPosition.AtBeginning:
                            return 1; 
                        case NewItemPlaceholderPosition.AtEnd: 
                            return InternalCount - 2;
                    } 
                }
            }
            else
            { 
                index = -1;
            } 
 
            return (index < 0 ) ? index : index + delta;
        } 

        // fix up CurrentPosition and CurrentItem after a collection change
        private void AdjustCurrencyForAdd(int index)
        { 
            if (InternalCount == 1)
            { 
                // added first item; set current at BeforeFirst 
                SetCurrent(null, -1);
            } 
            else if (index <= CurrentPosition)  // adjust current index if insertion is earlier
            {
                int newPosition = CurrentPosition + 1;
                if (newPosition < InternalCount) 
                {
                    // CurrentItem might be out of [....] if underlying list is not INCC 
                    // or if this Add is the result of a Replace (Rem + Add) 
                    SetCurrent(GetItemAt(newPosition), newPosition);
                } 
                else
                {
                    SetCurrent(null, InternalCount);
                } 
            }
        } 
 
        // fix up CurrentPosition and CurrentItem after a collection change
        private void AdjustCurrencyForRemove(int index) 
        {
            // adjust current index if deletion is earlier
            if (index < CurrentPosition)
            { 
                SetCurrent(CurrentItem, CurrentPosition - 1);
            } 
            // remember to move currency off the deleted element 
            else if (index == CurrentPosition)
            { 
                _currentElementWasRemoved = true;
            }
        }
 
        // fix up CurrentPosition and CurrentItem after a collection change
        private void AdjustCurrencyForMove(int oldIndex, int newIndex) 
        { 
            if (oldIndex == CurrentPosition)
            { 
                // moving the current item - currency moves with the item (bug 1942184)
                SetCurrent(GetItemAt(newIndex), newIndex);
            }
            else if (oldIndex < CurrentPosition && CurrentPosition <= newIndex) 
            {
                // moving an item from before current position to after - 
                // current item shifts back one position 
                SetCurrent(CurrentItem, CurrentPosition - 1);
            } 
            else if (newIndex <= CurrentPosition && CurrentPosition < oldIndex)
            {
                // moving an item from after current position to before -
                // current item shifts ahead one position 
                SetCurrent(CurrentItem, CurrentPosition + 1);
            } 
            // else no change necessary 
        }
 
        // fix up CurrentPosition and CurrentItem after a collection change
        private void AdjustCurrencyForReplace(int index)
        {
            // remember to move currency off the deleted element 
            if (index == CurrentPosition)
            { 
                _currentElementWasRemoved = true; 
            }
        } 

        // build the sort and filter information from the relevant properties
        private void PrepareSortAndFilter(IList list)
        { 
            // sort:  prepare the comparer
            if (_customSort != null) 
            { 
                ActiveComparer = _customSort;
            } 
            else if (_sort != null && _sort.Count > 0)
            {
                IComparer xmlComparer;
                if (AssemblyHelper.IsLoaded(UncommonAssembly.System_Xml) && 
                    (xmlComparer = PrepareXmlComparer(SourceCollection)) != null)
                { 
                    ActiveComparer = xmlComparer; 
                }
                else 
                {
                    ActiveComparer = new SortFieldComparer(_sort, Culture);
                }
            } 
            else
            { 
                ActiveComparer = null; 
            }
 
            // filter:  prepare the Predicate<object> filter
            ActiveFilter = Filter;
        }
 
        // set up the Xml comparer - code is isolated here to avoid loading System.Xml
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)] 
        private IComparer PrepareXmlComparer(IEnumerable collection) 
        {
            XmlDataCollection xdc = SourceCollection as XmlDataCollection; 
            if (xdc != null)
            {
                Invariant.Assert(_sort != null);
                return new XmlNodeComparer(_sort, xdc.XmlNamespaceManager, Culture); 
            }
            return null; 
        } 

        // set new SortDescription collection; rehook collection change notification handler 
        private void SetSortDescriptions(SortDescriptionCollection descriptions)
        {
            if (_sort != null)
            { 
                ((INotifyCollectionChanged)_sort).CollectionChanged -= new NotifyCollectionChangedEventHandler(SortDescriptionsChanged);
            } 
 
            _sort = descriptions;
 
            if (_sort != null)
            {
                Invariant.Assert(_sort.Count == 0, "must be empty SortDescription collection");
                ((INotifyCollectionChanged)_sort).CollectionChanged += new NotifyCollectionChangedEventHandler(SortDescriptionsChanged); 
            }
        } 
 
        // SortDescription was added/removed, refresh CollectionView
        private void SortDescriptionsChanged(object sender, NotifyCollectionChangedEventArgs e) 
        {
            if (IsAddingNew || IsEditingItem)
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringAddOrEdit, "Sorting"));
 
            // adding to SortDescriptions overrides custom sort
            if (_sort.Count > 0) 
            { 
                _customSort = null;
            } 

            RefreshOrDefer();
        }
 
        #region Grouping
 
        // divide the data items into groups 
        void PrepareGroups()
        { 
            // discard old groups
            _group.Clear();

            // initialize the synthetic top level group 
            _group.Initialize();
 
            // if there's no grouping, there's nothing to do 
            _isGrouping = (_group.GroupBy != null);
            if (!_isGrouping) 
                return;

            // reset the grouping comparer
            IComparer comparer = ActiveComparer; 
            if (comparer != null)
            { 
                _group.ActiveComparer = comparer; 
            }
            else 
            {
                CollectionViewGroupInternal.IListComparer ilc = _group.ActiveComparer as CollectionViewGroupInternal.IListComparer;
                if (ilc != null)
                { 
                    ilc.ResetList(InternalList);
                } 
                else 
                {
                    _group.ActiveComparer = new CollectionViewGroupInternal.IListComparer(InternalList); 
                }
            }

            // loop through the sorted/filtered list of items, dividing them 
            // into groups (with special cases for placeholder and new item)
            if (NewItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning) 
            { 
                _group.InsertSpecialItem(0, NewItemPlaceholder, true /*loading*/);
                if (IsAddingNew) 
                {
                    _group.InsertSpecialItem(1, _newItem, true /*loading*/);
                }
            } 

            for (int k=0, n=InternalList.Count;  k<n;  ++k) 
            { 
                object item = InternalList[k];
                if (!IsAddingNew || !Object.Equals(_newItem, item)) 
                {
                    _group.AddToSubgroups(item, true /*loading*/);
                }
            } 

            if (IsAddingNew && NewItemPlaceholderPosition != NewItemPlaceholderPosition.AtBeginning) 
            { 
                _group.InsertSpecialItem(_group.Items.Count, _newItem, true /*loading*/);
            } 
            if (NewItemPlaceholderPosition == NewItemPlaceholderPosition.AtEnd)
            {
                _group.InsertSpecialItem(_group.Items.Count, NewItemPlaceholder, true /*loading*/);
            } 
        }
 
 
        // For the Group to report collection changed
        void OnGroupChanged(object sender, NotifyCollectionChangedEventArgs e) 
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                AdjustCurrencyForAdd(e.NewStartingIndex); 
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove) 
            { 
                AdjustCurrencyForRemove(e.OldStartingIndex);
            } 
            OnCollectionChanged(e);
        }

        // The GroupDescriptions collection changed 
        void OnGroupByChanged(object sender, NotifyCollectionChangedEventArgs e)
        { 
            if (IsAddingNew || IsEditingItem) 
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringAddOrEdit, "Grouping"));
 
            // This is a huge change.  Just refresh the view.
            RefreshOrDefer();
        }
 
        // A group description for one of the subgroups changed
        void OnGroupDescriptionChanged(object sender, EventArgs e) 
        { 
            if (IsAddingNew || IsEditingItem)
                throw new InvalidOperationException(SR.Get(SRID.MemberNotAllowedDuringAddOrEdit, "Grouping")); 

            // This is a huge change.  Just refresh the view.
            RefreshOrDefer();
        } 

        // An item was inserted into the collection.  Update the groups. 
        void AddItemToGroups(object item) 
        {
            if (IsAddingNew && item == _newItem) 
            {
                int index;
                switch (NewItemPlaceholderPosition)
                { 
                    case NewItemPlaceholderPosition.None:
                    default: 
                        index = _group.Items.Count; 
                        break;
                    case NewItemPlaceholderPosition.AtBeginning: 
                        index = 1;
                        break;
                    case NewItemPlaceholderPosition.AtEnd:
                        index = _group.Items.Count - 1; 
                        break;
                } 
 
                _group.InsertSpecialItem(index, item, false /*loading*/);
            } 
            else
            {
                _group.AddToSubgroups(item, false /*loading*/);
            } 
        }
 
        // An item was removed from the collection.  Update the groups. 
        void RemoveItemFromGroups(object item)
        { 
            if (CanGroupNamesChange || _group.RemoveFromSubgroups(item))
            {
                // the item didn't appear where we expected it to.
                _group.RemoveItemFromSubgroupsByExhaustiveSearch(item); 
            }
        } 
 
        #endregion Grouping
 
        /// <summary>
        /// Helper to raise a PropertyChanged event  />).
        /// </summary>
        private void OnPropertyChanged(string propertyName) 
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName)); 
        } 

        #endregion Private Methods 



        //----------------------------------------------------- 
        //
        //  Private Fields 
        // 
        //-----------------------------------------------------
 
        #region Private Fields

        private IList               _internalList;
        private CollectionViewGroupRoot _group; 
        private bool                _isGrouping;
        private IComparer           _activeComparer; 
        private Predicate<object>   _activeFilter; 
        private SortDescriptionCollection  _sort;
        private IComparer           _customSort; 
        private ArrayList           _shadowCollection;
        private bool                _applyChangeToShadow = false;
        private bool                _currentElementWasRemoved;  // true if we need to MoveCurrencyOffDeletedElement
        private object              _newItem = NoNewItem; 
        private object              _editItem;
        private int                 _newItemIndex;  // position _newItem in the source collection 
        private NewItemPlaceholderPosition _newItemPlaceholderPosition; 
        private bool                _isItemConstructorValid;
        private ConstructorInfo     _itemConstructor; 

        private const int           _unknownIndex = -1;

        #endregion Private Fields 
    }
 
    /// <summary> 
    /// A delegate to select the group description as a function of the
    /// parent group and its level. 
    /// </summary>
    public delegate GroupDescription GroupDescriptionSelectorCallback(CollectionViewGroup group, int level);

} 


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
