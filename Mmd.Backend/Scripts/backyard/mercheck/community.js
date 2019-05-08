//////翻页按钮
function onLeftClick() {
    var currentPageNumber = parseInt($('#currentPage').text());
    if (currentPageNumber === 1)
        alert("已经是第1页了！");
    else {
        refresh(--currentPageNumber);
        $('#currentPage').text(currentPageNumber);
    }
}

function onRightClick() {
    var currentPageNumber = parseInt($('#currentPage').text());
    var totalPages = parseInt($("#totalPages").text());
    if (currentPageNumber === totalPages || totalPages === 0)
        alert("已经没有下一页了！");
    else {
        refresh(++currentPageNumber);
        $('#currentPage').text(currentPageNumber);
    }
}

function onJump() {
    try {
        //判断是否为数字
        if (!$('#page_count').val() || isNaN($('#page_count').val())) {
            alert("请输入数字！");
        }
        else {
            var jumpToNumber = parseInt($('#page_count').val());
            var totalPages = parseInt($("#totalPages").text());
            if (jumpToNumber > totalPages)
                alert("超过最大页数！");
            else {
                refresh(jumpToNumber);
                $('#currentPage').text(jumpToNumber);
            }
        }
    } catch (e) {
        alert(e);
    }
}

function refresh(pageIndex) {
    var s_proname = $("#s_proname").val();
    $.ajax({
        type: "GET",
        url: "/Community/GetCommunityList?pageIndex=" + parseInt(pageIndex) + "&pageSize=" + "10" + "&query=" + s_proname,
        data: {},
        datatype: "json",
        success: function (data) {
            $("#list_table").html(data);
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });

}

function imgView(obj) {
    var str = $(obj).parent().data("img");
    var arr = str.split(',');
    var html = '';
    for (var i in arr) {
        html += '<div><a data-imgurl="' + arr[i] + ' " href="javascript:;"><img src="' + arr[i] + '" /></a></div>';
    }
    $(".gallery").html(html).show();
    $(".maskLayer").show();
    $(".inputText-Tap").show();
    $('.zoom, .gallery a').on('click', open);
}
function onDel(obj, id) {
    if (!confirm("确定要删除吗?")) {
        return;
    }
    $.ajax({
        type: "POST",
        url: "/Comment/DelCommunity",
        data: { cid: id },
        success: function (res) {
            if (res.Status == "Success") {
                alert("删除成功！");
            } else if (res.Status == "Fail") {
                alert(res.message);
            }
            //刷新到第一页
            refresh(1);
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });
}
function onForbid(obj,uid) {
    var username = $(obj).parents("tr").find(".username").text();
    $('#tan_gname').val(username);
    $("#uid").val(uid);
    $(".alertForm,.maskLayer").css("display", "block");
    $(".alertForm .inputText-Tap,.alertForm .inputBtn-Tap").show();
    $(".i-close").on("click", function () { $(".alertForm,.maskLayer,.alertForm .inputBtn-Tap").hide(); $("#dayscount").val(1) });
}
function forbid() {
    var uid = $("#uid").val();
    var days = $("#dayscount").val();
    if (!days || isNaN(days)) {
        return;
    }
    var dayCount = parseInt(days);
    $.ajax({
        type: "POST",
        url: "/Community/Forbidden",
        data: { uid: uid, days: dayCount },
        success: function (res) {
            if (res.Status == "Success") {
                alert("禁言成功！");
            } else if (res.Status == "Fail") {
                alert(res.message);
            }
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });
}
function onSearch() {
    refresh(1);
}
var zoom, zoomContent;
$(document).ready(function () {
    init(jQuery);
    zoom = $('#zoom').hide();
    zoomContent = $('#zoom .content');
    $(".i-close").click(function () {
        $(".gallery").html("").hide();
        $(".maskLayer").hide();
        $(".inputText-Tap").hide();
    });
});
var overlay = '<div class="overlay"></div>',
	    zoomedIn = false,
	    openedImage = null;
function open() {
    var link = $(this),
        src = link.data('imgurl');
    if (!src) {
        return;
    }
    var  windowWidth = $(window).width(),
	    windowHeight = $(window).height();
    $('#zoom .previous, #zoom .next').show();
    if (link.hasClass('zoom')) {
        $('#zoom .previous, #zoom .next').hide();
    }
    var body = $('body');
    if (!zoomedIn) {
        zoomedIn = true;
        zoom.show();
        body.addClass('zoomed');
    }
    
    var image = $(new Image()).hide().css({ width: 'auto' });
    body.append(image);
    //zoomContent.html('').delay(500).addClass('loading');
    zoomContent.prepend(overlay);
    image.load(render).attr('src', src);
    openedImage = link;

    function render() {
        var image = $(this),
            borderWidth = parseInt(zoomContent.css('borderLeftWidth')),
            maxImageWidth = windowWidth - (borderWidth * 2),
            maxImageHeight = windowHeight - (borderWidth * 2),
            imageWidth = image.width(),
            imageHeight = image.height();
        if (imageWidth == zoomContent.width() && imageWidth <= maxImageWidth && imageHeight == zoomContent.height() && imageHeight <= maxImageHeight) {
            show(image);
            return;
        }
        if (imageWidth > maxImageWidth || imageHeight > maxImageHeight) {
            var desiredHeight = maxImageHeight < imageHeight ? maxImageHeight : imageHeight,
                desiredWidth = maxImageWidth < imageWidth ? maxImageWidth : imageWidth;
            if (desiredHeight / imageHeight <= desiredWidth / imageWidth) {
                image.width(Math.round(imageWidth * desiredHeight / imageHeight));
                image.height(desiredHeight);
            } else {
                image.width(desiredWidth);
                image.height(Math.round(imageHeight * desiredWidth / imageWidth));
            }
        }
        zoomContent.animate({
            width: image.width(),
            height: image.height(),
            marginTop: -(image.height() / 2) - borderWidth,
            marginLeft: -(image.width() / 2) - borderWidth
        }, 100, function () {
            show(image);
        });

        function show(image) {
            zoomContent.html(image);
            image.show();
            zoomContent.removeClass('loading');
        }
    }
}