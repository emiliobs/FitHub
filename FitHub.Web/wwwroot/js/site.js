// --- SUCCESS MESSAGE: FitHub Vibrant Orange/Green ---
function showSuccess(message) {
    Swal.fire({
        icon: 'success',
        title: '<h3 style="color: #ffffff; font-weight: 800;">SUCCESS!</h3>',
        text: message,
        background: '#121212',
        iconColor: '#28a745',   // Vibrant Green for Success
        confirmButtonColor: '#28a745',
        confirmButtonText: 'GREAT!',
        color: '#ffffff',       // Pure white for better readability
        customClass: {
            popup: 'border border-success shadow-lg'
        }
    });
}

// --- ERROR MESSAGE: High Contrast Red ---
function showError(message) {
    Swal.fire({
        icon: 'error',
        title: '<h3 style="color: #ffffff; font-weight: 800;">ACTION BLOCKED</h3>',
        text: message,
        background: '#0a0a0a', // Deeper black for more contrast
        iconColor: '#ff3b3b',   // Vibrant Neon Red
        confirmButtonColor: '#ff3b3b', // Solid Red Button (Now visible!)
        confirmButtonText: 'UNDERSTOOD',
        color: '#e0e0e0',       // Light grey text
        customClass: {
            popup: 'border border-danger shadow-neon-red animate__animated animate__shakeX'
        }
    });
}

// --- CONFIRM DELETE: Ultra High Impact ---
function confirmDelete(callback) {
    Swal.fire({
        title: '<h2 style="color: #ffffff; font-weight: 900; letter-spacing: -1px;">DANGER ZONE</h2>',
        html: '<p style="color: #e0e0e0;">This action is <b style="color: #ff3b3b; text-decoration: underline;">irreversible</b>. Are you sure you want to proceed?</p>',
        icon: 'warning',
        iconColor: '#ff3b3b',
        background: '#050505',
        showCancelButton: true,
        confirmButtonColor: '#ff3b3b', // Solid Red
        cancelButtonColor: '#333333',  // Dark Grey (Not invisible black)
        confirmButtonText: '<i class="bi bi-trash-fill"></i> DELETE NOW',
        cancelButtonText: 'BACK TO SAFETY',
        color: '#ffffff',
        customClass: {
            popup: 'border border-danger animate__animated animate__pulse'
        }
    }).then((result) => {
        if (result.isConfirmed) {
            callback();
        }
    });
}