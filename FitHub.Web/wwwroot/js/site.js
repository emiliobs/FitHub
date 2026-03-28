// SUCCESS MESSAGE - SweetAlert Success
// Mensaje de éxito optimizado con los colores de FitHub
function showSuccess(message) {
    Swal.fire({
        icon: 'success',
        title: 'Success',
        text: message,
        confirmButtonColor: '#FF6B00' // FitHub Orange
    });
}

// ERROR MESSAGE - SweetAlert Error
// Mensaje de error para excepciones capturadas
function showError(message) {
    Swal.fire({
        icon: 'error',
        title: 'Error',
        text: message,
        confirmButtonColor: '#1A1A1A' // FitHub Carbon Black
    });
}

// CONFIRM DELETE - Reusable with callback
// Confirmación de eliminación reutilizable
function confirmDelete(callback) {
    Swal.fire({
        title: 'Are you sure?',
        text: "This action cannot be undone!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#FF6B00',
        cancelButtonColor: '#1A1A1A',
        confirmButtonText: 'Yes, delete it!',
        cancelButtonText: 'Cancel'
    }).then((result) => {
        if (result.isConfirmed) {
            // Execute the logic passed as parameter
            callback();
        }
    });
}