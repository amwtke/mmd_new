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
        if (isNaN($('#page_count').val())) {
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
    //var s_mername = $("#s_mername").val();
    var s_proname = $("#s_proname").val();
    $.ajax({
        type: "GET",
        url: "/Comment/GetList?pageIndex=" + parseInt(pageIndex) + "&mer=" + "" + "&pro=" + s_proname,
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
//评论
function onComment(obj, id) {
    var proname = $(obj).parents("tr").find(".proname").text();
    var mid = $(obj).parents("tr").find(".mername").data("mid");
    $("#mid").val(mid);
    $('#tan_gname').val(proname);
    $("#commentid").val(id);
    $(".alertForm,.maskLayer").css("display", "block");
    $(".inputBtn-Tap").show();
    $(".i-close").on("click", function () { $(".alertForm,.maskLayer").css("display", "none"); });
}
function postComment() {
    var comment = $('#tan_url').val();
    var mid = $("#mid").val();
    if (comment) {
        var id = $("#commentid").val();
        $.ajax({
            type: "POST",
            url: "/Comment/AddComment",
            data: { pid: id, commentContent: comment, mid: mid },
            success: function (res) {
                if (res.Status == "Success") {
                    alert("保存成功！");
                } else if (res.Status == "Fail") {
                    alert(res.message);
                }
                //刷新到第一页
                refresh(1);
                $('#tan_gname').val("");
                $("#commentid").val("");
                $('#tan_url').val("");
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                alert(errorThrown);
            }
        });
        $(".alertForm,.maskLayer").css("display", "none");
    } 
}
function onSearch() {
    refresh(1);
}

//充值关闭
function closeClick() {
    $("#ADialog,.masklayer").css("display", "none");
    $("#BDialog,.masklayer").css("display", "none");
}
