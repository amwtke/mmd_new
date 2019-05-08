$(document).ready(function() {
    Init_Upload();
});
function Init_Upload() {
    $('#FormUpload input[name=UploadedFile]').change(function (evt) { singleFileSelected(evt); });
    //$("#FormUpload button[id=Cancel_btn]").click(function () {
    //    Cancel_btn_handler()
    //});
    $('#FormUpload button[class=inputBtn-Tap]').click(function () {
        UploadFile();
    });

    InitImageBox();
}



function InitImageBox() {
    var imgName = $('#biz_licence_url').val();
    if (imgName) {
        if (imgName.indexOf("mmpintuan") > 0) {
            $("#cross").remove();//删除加号
            $(".Add-ImgCuber").append("<img src=\"" + imgName + "\" class=\"thumb\">");//添加图片链接
        }
    }
}

function singleFileSelected(evt) {
    //var selectedFile = evt.target.files can use this  or select input file element and access it's files object
    var selectedFile = ($('#FormUpload input[name = UploadedFile]'))[0].files[0];//FileControl.files[0];

    if (selectedFile) {
        var FileSize = 0;
        var imageType = /image.*/;
        //if (selectedFile.size > 1048576) {
        //    FileSize = Math.round(selectedFile.size * 100 / 1048576) / 100 + " MB";
        //}
        //else if (selectedFile.size > 1024) {
        //    FileSize = Math.round(selectedFile.size * 100 / 1024) / 100 + " KB";
        //}
        //else {
        //    FileSize = selectedFile.size + " Bytes";
        //}

        if (selectedFile.type.match(imageType) && selectedFile.size<=2097152) {
            var reader = new FileReader();
            reader.onload = function(e) {

                $(".thumb").remove();//删除缩略图
                $("#cross").remove();//删除加号
                var dataURL = reader.result;
                var img = new Image();
                img.src = dataURL;
                img.className = "thumb";

                $(".Add-ImgCuber").append(img);
            };
            reader.readAsDataURL(selectedFile);
            $('#FormUpload input[name=biz_licence_url]').val(selectedFile.name);
            //$("#FileType").text("type : " + selectedFile.type);
            //$("#FileSize").text("Size : " + FileSize);
        } else {
            if (!selectedFile.type.match(imageType)) {
                alert("只能录入图片文件！");
                $('#FormUpload input[name = UploadedFile]').val("");
            }
                
            if (selectedFile.size > 2097152) {
                alert("上传图片大小不能超过2Mb！");
                $('#FormUpload input[name = UploadedFile]').val("");
            }
            $('#FormUpload input[name=biz_licence_url]').val("");
            $(".thumb").remove();
            //如果没有加号则加上，防止多加
            if ($('#cross').length === 0) {
                $(".Add-ImgCuber").append("<i id=\"cross\" class=\"MMda i-add pdr5 green\" style=\"margin-top: 72px;\"></i>");
            }
        }
    }
}

function UploadFile() {
    // we can create form by passing the form to Constructor of formData obeject
    //or creating it manually using append function  but please note file file name should be same like the action Paramter
    //var dataString = new FormData();
    //dataString.append("UploadedFile", selectedFile);

    var form = $("#FormUpload")[0];
    var dataString = new FormData(form);
    $.ajax({
        url: "/Upload/Uploader",  //Server script to process data
        type: 'POST',
        //xhr: function () {  // Custom XMLHttpRequest
        //    var myXhr = $.ajaxSettings.xhr();
        //    if (myXhr.upload) { // Check if upload property exists
        //        //myXhr.upload.onprogress = progressHandlingFunction
        //        myXhr.upload.addEventListener('progress', progressHandlingFunction, false); // For handling the progress of the upload
        //    }
        //    return myXhr;
        //},
        //Ajax events
        success: successHandler,
        error: errorHandler,
        complete: completeHandler,
        // Form data
        data: dataString,
        //Options to tell jQuery not to process data or worry about content-type.
        cache: false,
        contentType: false,
        processData: false
    });
}



