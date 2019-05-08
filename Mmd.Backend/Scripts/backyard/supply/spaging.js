//////翻页按钮
function onLeftClick() {
    var currentPageNumber = parseInt($('#currentPage').text());
    if (currentPageNumber === 1)
        alert("已经是第1页了！");
    else {
        refreshSupple(--currentPageNumber);
        $('#currentPage').text(currentPageNumber);
    }
}

function onRightClick() {
    var currentPageNumber = parseInt($('#currentPage').text());
    var totalPages = parseInt($("#totalPages").text());
    if (currentPageNumber === totalPages || totalPages === 0)
        alert("已经没有下一页了！");
    else {
        refreshSupple(++currentPageNumber);
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
                refreshSupple(jumpToNumber);
                $('#currentPage').text(jumpToNumber);
            }
        }
    } catch (e) {
        alert(e);
    }
}

function refreshSupple(pageCount) {
    var q = $('#s_text').val();
    var brand = $("#xxkPP").find(".acti").attr("code");
    var category = $("#xxkPL").find(".acti").attr("code");
    $.ajax({
        type: "GET",
        url: "/Supply/SupplyListPartial_Backend?pageIndex=" + parseInt(pageCount) + "&q=" + q + "&category=" + category + "&brand=" + brand,
        data: {},
        datatype: "json",
        success: function (data) {
            $(".Lorelei-commonTable").html(data);
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert(errorThrown);
        }
    });
}

// sid = item.sid, status = (int)ESupplyStatus.已删除 
function DelConfirm(sid,status) {
    if (confirm("您确定要删除吗？")) {
        $.ajax({
            type: "GET",
            url: "/Supply/UpdateSupplystatus?sid=" + sid + "&status=" + status,
            data: {},
            datatype: "json",
            success: function (data) {
                alert('删除成功');
                refreshSupple(1);
            },
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                alert(errorThrown);
            }
        });
    }
}