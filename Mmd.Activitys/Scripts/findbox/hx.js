$(function(){
	//1核销成功，2不可重复领宝
	var datas = null;
	var arr = [
		'images/bg.png',
		'images/okbx.png',
		'images/lbcg.png',
		'images/bkcf.png'
	];
	var Img = new Image();
	function loadImg(length){
		if(length==0){
			$(".loging").remove();
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
        type:'POST',
        url: "/FindBox/hexiao?utid=1fd237eb-0571-483d-bd4e-2260c77a985a&openid=otuH9sv6Vmp32_NBed9eNl3Bm0p8",
		dataType:"json",
		success:function(data){
			if(data.CustomStatus=="Success"){
				datas = data.Message;
				$(".fxbxBox p").eq(0).html("宝贝："+datas.prize);
				$(".fxbxBox p").eq(0).css("color","#d13030");
				$(".fxbxBox p").eq(1).html("宝贝说明："+datas.state);
				$(".fxbxBox p").eq(2).html("领宝最后期限："+datas.endTime);
				if(datas.step==2){
					$(".dkbx img").attr('src',"images/bkcf.png")
				}
			}else{
				alert(data.Message);
				return false;
			}
		},
		error:function(a,b,c){
			if(a.status!=200){
				alert("请求"+a.status);
			}else{
				alert("返回结果异常！");
			}
			return false;
		}
	});
});