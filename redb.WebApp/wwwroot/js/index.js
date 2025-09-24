$(document).ready(function () {

    let toogleDivTxt = (e) => {
        let sh = Number($(e)[0].scrollHeight.toFixed());
        let ah = Number($(e).height().toFixed());
        if (ah < sh) {
            $(e).animate({ "height": sh }, 800);
        }
        else if (ah >= sh) {
            $(e).animate({ "height": 200 }, 800);
        }
    }
    $(".div_toggle").click(function (e) {
        e.preventDefault();
        toogleDivTxt('#div_text_' + e.target.id.slice(-1));
    });
});