mergeInto(LibraryManager.library, {

    HideLoading: function(){
        document.getElementById("loading-overlay").style.display = "none";
    },

    ShowToast: function(noti){
        const notiStr = UTF8ToString(noti)
        showNoti(notiStr);
    }
 
});