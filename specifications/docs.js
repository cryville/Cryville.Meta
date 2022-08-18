window.onload = function () {
	var toc = $("#toc");
	var counter = [0, 0, 0, 0, 0]; var excounter = 0;
	var dl = [toc, null, null, null, null];
	var dd = [null, null, null, null, null];
	$("<hr>").insertAfter($("h1"));
	$("#body").find("h2, h3, h4, h5, h6").each(function (i) {
		if ($(this).hasClass("xxtoc")) return;
		var flag = $(this).parent().is("#foot");
		var l = parseInt(this.tagName.substring(1)) - 2;
		if (flag) excounter++;
		else {
			counter[l]++;
			for (var j = l + 1; j < 5; j++) {
				counter[j] = 0;
				dl[j] = null;
			}
		}
		if (!dl[l]) dl[l] = $("<dl></dl>").appendTo(dd[l - 1]);
		if (flag) {
			var id1 = "section-ex" + excounter;
			this.id = id1;
			dd[l] = $("<dd></dd>")
				.append($("<a></a>").text($(this).text()).attr("href", "#" + id1))
				.appendTo(dl[l]);
		}
		else {
			var it = counter[0].toString();
			for (var k = 1; k <= l; k++) it += "." + counter[k].toString();
			var id2 = "section-" + it;
			this.id = id2;
			dd[l] = $("<dd></dd>")
				.append($("<a></a>").text(it + ". " + $(this).text()).attr("href", "#" + id2))
				.appendTo(dl[l]);
		}
	});
	var cite = $("#cite");
	$("cite").each(function (i) {
		var el = $(this);
		var index = i + 1;
		el.attr("id", "cited-" + index);
		$("<sup></sup>").append($("<a></a>").text("[" + (i + 1) + "]").attr("href", "#cite-" + index)).insertAfter(el);
		var linkel = el.children("a");
		var link = null;
		if (linkel.length) link = linkel.attr("href");
		var li = $("<li></li>").attr("id", "cite-" + index).append($("<a>").attr("href", "#cited-" + index).text(el.attr("title")));
		if (link) li.append($("<a></a>").text(link).attr("href", link));
		cite.append(li);
	});
	$(".figgroup-horiz").each(function (i) {
		var g = $(this);
		var figs = g.children("figure");
		var bw = 100 / figs.length;
		figs.css("width", "calc(" + bw.toString() + "% - " + g.css("padding-left") + " - " + g.css("padding-right") + ")");
	});
	var now = new Date();
	$("time.now").attr("datetime", now.toISOString()).text(now.toString());
};

window.onerror = function (m, s, l, c, e) { alert(m + "\nat " + s + " (" + l + ":" + c + ")"); };