// --- SUCCESS MESSAGE: FitHub Vibrant Orange/Green ---
function showSuccess(message) {
    Swal.fire({
        icon: 'success',
        title: '<h3 style="color: #ffffff; font-weight: 800;">SUCCESS!</h3>',
        text: message,
        background: 'rgba(17, 24, 36, 0.92)',
        iconColor: '#b7c5d9',
        confirmButtonColor: '#a5b5c9',
        confirmButtonText: 'GREAT!',
        color: '#ebf0f7',
        customClass: {
            popup: 'fithub-delete-popup'
        }
    });
}

// --- ERROR MESSAGE: High Contrast Red ---
function showError(message) {
    Swal.fire({
        icon: 'error',
        title: '<h3 style="color: #ffffff; font-weight: 800;">ACTION BLOCKED</h3>',
        text: message,
        background: 'rgba(17, 24, 36, 0.92)',
        iconColor: '#cf8b99',
        confirmButtonColor: '#c77a89',
        confirmButtonText: 'UNDERSTOOD',
        color: '#ebf0f7',
        customClass: {
            popup: 'fithub-delete-popup animate__animated animate__shakeX'
        }
    });
}

// --- CONFIRM DELETE: Ultra High Impact ---
function confirmDelete(callback) {
    Swal.fire({
        title: '<h2 style="color: #ffffff; font-weight: 900; letter-spacing: -1px;">DANGER ZONE</h2>',
        html: '<p style="color: #ebf0f7;">This action is <b style="color: #cf8b99; text-decoration: underline;">irreversible</b>. Are you sure you want to proceed?</p>',
        icon: 'warning',
        iconColor: '#cf8b99',
        background: 'rgba(17, 24, 36, 0.94)',
        showCancelButton: true,
        confirmButtonColor: '#c77a89',
        cancelButtonColor: '#42516a',
        confirmButtonText: '<i class="bi bi-trash-fill"></i> DELETE NOW',
        cancelButtonText: 'BACK TO SAFETY',
        color: '#ebf0f7',
        customClass: {
            popup: 'fithub-delete-popup animate__animated animate__pulse'
        }
    }).then((result) => {
        if (result.isConfirmed) {
            callback();
        }
    });
}