// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
//$(document).ready(function () {

//    // Ֆունկցիա մոդալի բացման համար
//    function openModal() {
//        $("#appointmentModal").modal("show");
//        $.ajax({
//            type: "GET",
//            url: "/Appointments/GetCreatePartialView",
//            success: function (data) {
//                $(".modal-body").html(data);
//                loadHourOptions(); // Load hour options after PartialView is loaded
//            },
//            error: function () {
//                alert("Սխալ է առաջացել");
//            }
//        });
//    }

//    // Ֆունկցիա ժամերի ցանկը լցնելու համար
//    function loadHourOptions() {
//        var hourOptionsUrl = '@Url.Action("GetHourOptions", "Appointments")';
//        $.ajax({
//            type: 'GET',
//            url: '@getHourOptionsUrl',
//            success: function (data) {
//                var dropdown = $('#AppointmentHour');
//                dropdown.empty();
//                $.each(data, function (index, option) {
//                    dropdown.append($('<option></option>').attr('value', option.value).text(option.text));
//                });
//            }
//        });
//    }
//});