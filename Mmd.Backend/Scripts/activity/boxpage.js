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

function changeStatus(obj) {
    var id = $(obj).data("bid");
    var status = $(obj).text();
    var s = 1;
    if (status == "下线") {
        if (confirm("确定要下线当前活动吗？")) {
            s = 2;
        } else {
            return;
        }
    }  
    if (status == "已下线") return;
    $.ajax({
        type: "post",
        url: "/Box/ChangeStatus",
        data: { id: id, status: s },
        success: function (res) {
            if (res.message && res.message == "Success") {
                //if (s == 1) $(obj).text("下线");
                //else $(obj).text("已下线");
                alert("保存成功");
                refresh(1);
            } else {
                alert("保存失败");
                console.log(res.message);
            }
        }
    })
}

function refresh(pageIndex) {
        $.ajax({
            type: "GET",
            url: "/Box/BoxListPartial?pageIndex=" + parseInt(pageIndex) + "&pageSize=20" + "&q=" + "&status=" + 0,
            data: {},
            success: function (data) {
                $("#list_table").html(data);
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                alert(errorThrown);
            }
        });
    }