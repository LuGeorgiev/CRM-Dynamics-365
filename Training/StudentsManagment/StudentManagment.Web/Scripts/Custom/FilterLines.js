$(document).ready(function () {

    $("#myInput").on("keyup", function () {
        let textToSearch = $(this).val().toLowerCase();

        $("#myTable tr .case-title").filter(function () {
            $(this).parent().toggle($(this).text().toLowerCase().indexOf(textToSearch) > -1);
        });

    });
});