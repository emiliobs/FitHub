// SUCCESS MESSAGE - FitHub SweetAlert Success
// Mensaje de éxito optimizado con los colores de FitHub
function showSuccess(message) {
    Swal.fire({
        icon: 'success',
        title: '<span style="color: #ffffff">Success</span>',
        text: message,
        background: '#121212', // Dark background
        iconColor: '#FF6B00',   // Neon Orange
        confirmButtonColor: '#FF6B00',
        color: '#b0b0b0',       // Silver text
        customClass: {
            popup: 'border-fithub-neon'
        }
    });
}

// ERROR MESSAGE - FitHub SweetAlert Error
// Mensaje de error para excepciones capturadas
function showError(message) {
    Swal.fire({
        icon: 'error',
        title: '<span style="color: #ffffff">Error</span>',
        text: message,
        background: '#121212',
        iconColor: '#dc3545',   // Neon Red
        confirmButtonColor: '#1A1A1A', // Carbon Black
        color: '#b0b0b0',
        customClass: {
            popup: 'border-fithub-red'
        }
    });
}

// CONFIRM DELETE - Ultra Modern FitHub Red Style
// Confirmación de eliminación con estética de alto impacto
function confirmDelete(callback) {
    Swal.fire({
        title: '<h2 style="color: #ffffff; font-weight: 800; letter-spacing: -1px;">ARE YOU SURE?</h2>',
        html: '<p style="color: #b0b0b0;">This action is <b style="color: #ff3b3b;">permanent</b>. The Warrior will be removed from the system.</p>',
        icon: 'warning',
        iconColor: '#ff3b3b', // Vibrant Neon Red
        background: '#0a0a0a', // Deep Carbon Black
        showCancelButton: true,
        confirmButtonColor: '#d33', // Action Red
        cancelButtonColor: '#1A1A1A', // FitHub Grey/Black
        confirmButtonText: '<i class="bi bi-trash-fill"></i> YES, DELETE IT!',
        cancelButtonText: 'CANCEL',
        customClass: {
            popup: 'fithub-delete-popup animate__animated animate__pulse'
        }
    }).then((result) => {
        if (result.isConfirmed) {
            callback();
        }
    });
}