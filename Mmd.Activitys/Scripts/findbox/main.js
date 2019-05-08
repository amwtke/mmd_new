$(function(){
    //0未寻宝，1已寻宝，2已领奖,-1,已过期
	var arr = [
		'/images/findbox/bg.png',
		'/images/findbox/dd1.png',
		'/images/findbox/dd2.png',
		'/images/findbox/end.png',
		'/images/findbox/gmq1.png',
		'/images/findbox/gmq2.png',
		'/images/findbox/goindex.png',
		'/images/findbox/kbao.png',
		'/images/findbox/lb.png',
		'/images/findbox/nobx.png',
		'/images/findbox/okbx.png',
		'/images/findbox/openbx.png',
		'/images/findbox/succEnd.png',
		'/images/findbox/sy1.png',
		'/images/findbox/sy2.png',
		'/images/findbox/w1.png',
		'/images/findbox/w2.png',
		'/images/findbox/xhz1.png',
		'/images/findbox/xhz2.png',
		'/images/findbox/yl.png'
	];
	var isLogin = false;
	var isAjax  = null;
	var Img = new Image();
	function loadImg(length){
		if(length==0){
			isLogin = true;
			return true;
		}
		Img.src =  arr[arr.length-length];
		Img.onload = function(){
			arr.splice(0,1);
			loadImg(arr.length);
		}
	};
	loadImg(arr.length);
	$.ajax({
	    type: 'POST',
	    url: "/FindBox/IsExistsBox?appid=wxa2f78f4dfc3b8ab6&openid=otuH9smsfurYNZnos2BSOYiZ1Znk",
		dataType:"json",
		success: function (Data) {
		    if (Data != "") {
		        isAjax = Data;
		        if (Data.glist.length > 0)
		        {
		            var html = "";
		            $.each(Data.glist, function (index,obj) {
		                html += '<div class="listBox"> <div class="name">' + obj.name + '</div><div class="prize">' + obj.prize + '</div></div>';
		            });
		            $(".zjList").html(html);
		        }
			}else{
		        alert(Data);
				return false;
			}
		},
		error:function(a,b,c){
			if(a.status!=200){
				alert("请求"+a.status);
			}else{
				alert("返回结果异常2！"+b);
			}
			return false;
		}
	});
	function obj(){
		$(".p2").css("font-size","26px");
		$(".p4").css("display","none");
		if(isAjax.stock==0){
			$(".bxBox img").attr("src","/images/findbox/kbao.png");
			$(".bxBox").removeClass("bxNok");
			$(".p1").html("来迟一步，宝箱已经空了，");
			$(".p2").html("再去别的地方找找吧！");
			$(".dkbx img").attr("src", "/images/findbox/goindex.png");
		}
		if(isAjax.step==1 || isAjax.step==2){
		    $(".bxBox img").attr("src", "/images/findbox/okbx.png");
			$(".bxBox").removeClass("bxNok");
			$(".p1").html("恭喜您获得"+isAjax.prize);
			$(".p1").css("color","#d13030");
			$(".p2").css("font-size","14px");
			$(".p2").html("宝贝说明："+isAjax.state);
			$(".p4").css("display","block");
			$(".line").css("display","block");
			$(".zjListBox").css("display","block");
			if(isAjax.step==1){
			    $(".dkbx img").attr("src", "/images/findbox/lb.png");
			}else{
			    $(".dkbx img").attr("src", "/images/findbox/yl.png");
			}
		}
	}
	var time  = setInterval(function(){
		if(isLogin==true && isAjax){
			obj();
			$(".loging").remove();
			clearInterval(time);
		}
	},500);
	$(".dkbx").click(function(){
		if(isAjax.step == 0 && isAjax.stock>0){
			$(".bxBox").removeClass('bxNok');
			$(".bxBox").addClass("bxOkk");
			$.ajax({
			    type: 'POST',
			    url: "/FindBox/openBox?bid=" + isAjax.bid + "&appid=wxa2f78f4dfc3b8ab6&openid=otuH9smsfurYNZnos2BSOYiZ1Znk",
				dataType:"json",
				success:function(data){
					if(data!=""){
						isAjax = data;
						$(".bxBox img").animate({opacity:0.5,},1000,function(){
							$(".bxBox").removeClass('bxOkk');
							$(".bxBox img").attr('src', "/images/findbox/okbx.png");
							$(".bxBox img").css("opacity",'1');
							obj();
						});
					}else{
						alert(data);
					}
				},
				error:function(a,b,c){
					if(a.status!=200){
						alert("请求"+a.status);
					}else{
						alert("返回结果异常1！");
					}
					return false;
				}
			});
		}
		if(isAjax.step == 1){
		    $(".popupBOx").show();
			$("#code").qrcode({ 
			    render: "canvas",
			    width: $(".erweima").width(),
			    height:$("#code").width(),
			    text: isAjax.url
			});
		}
		if(isAjax.step == 2){
			return false;
		}
		if(isAjax.stock == 0){
			alert("返回首页");
		}
	});
	//关闭按钮
	$(".endBtn").click(function(){
		$(".popupBOx").hide();
		$('#code').html("");
	});
	//测试核销
	$(".popupBoxs").click(function(){
		location.href = "http://localhost:8080/gjx/xunbao/hexiao.html"
	});
});