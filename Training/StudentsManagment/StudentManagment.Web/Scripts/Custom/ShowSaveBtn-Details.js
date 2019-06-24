$(document).ready(function () {
    $('#details-first-name,#details-last-name,#details-student-status,#details-program,#details-program-advisor').prop('disabled', true);
    $(':input[type="submit"]').hide();

    $('#details-edit').click(function () {
        $('#details-first-name,#details-last-name').prop('disabled', false);
        $(':input[type="submit"]').show();
    });
});